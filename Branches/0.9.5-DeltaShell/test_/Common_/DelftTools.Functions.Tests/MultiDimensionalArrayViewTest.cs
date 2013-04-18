using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using log4net;
using log4net.Config;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class MultiDimensionalArrayViewTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (MultiDimensionalArrayViewTest));

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            LogHelper.ResetLogging();
        }


        [Test]
        public void Add()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            IMultiDimensionalArrayView view = new MultiDimensionalArrayView(array, 0, int.MinValue, int.MaxValue);
            view.Add(4);
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(1, array.Count);
        }

        [Test]
        public void ClearView()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int> {1, 2, 3, 4, 5};
            IMultiDimensionalArrayView view = new MultiDimensionalArrayView(array, 0, 1, 2);
            view.Clear();
            Assert.AreEqual(0, view.Count);
            Assert.AreEqual(3, array.Count);
            Assert.IsTrue(new[] {1, 4, 5}.SequenceEqual(array));
        }

        [Test]
        public void ConvertToListUsingCopyConstructor()
        {
            MultiDimensionalArray array = new MultiDimensionalArray<double>(2);
            IMultiDimensionalArray<double> view = new MultiDimensionalArrayView<double>(array);
            view[0] = 10;
            view[1] = 5;
            var arrayList = new ArrayList(view);
            Assert.AreEqual(10, arrayList[0]);
            Assert.AreEqual(5, arrayList[1]);
        }

        [Test]
        public void ConvertToListUsingCopyConstructorGeneric()
        {
            IList<int> values = new List<int> {1, 2, 3, 4};

            MultiDimensionalArray array = new MultiDimensionalArray<int>(values, new[] {1, 4});

            IMultiDimensionalArray<int> view = new MultiDimensionalArrayView<int>(array);

            IList array1D = new List<int>(view);

            Assert.AreEqual(4, array1D.Count);
            Assert.AreEqual(4, array1D[3]);
            Assert.AreEqual(2, array1D[1]);
        }

        [Test]
        public void Count()
        {
            //array = 1,2,3,4,5
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int> {1, 2, 3, 4, 5};
            //view  = 2,3
            IMultiDimensionalArrayView<int> view = new MultiDimensionalArrayView<int>(array, 0, 1, 2);
            Assert.AreEqual(2, view.Count);
        }

        [Test]
        public void Create()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(new[] {2, 2});
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 3;
            array[1, 1] = 4;

            IMultiDimensionalArrayView view = new MultiDimensionalArrayView();
            view.Parent = array;

            //why not same?? If i resize parent i do expect the subarray to be resize as well.
            //the values should be the same but not the reference
            Assert.AreNotSame(array.Shape, view.Shape);
            Assert.IsTrue(view.Shape.SequenceEqual(array.Shape));
            Assert.AreEqual(array.Count, view.Count);
            Assert.AreEqual(array.Rank, view.Rank);
            Assert.AreEqual(array.DefaultValue, view.DefaultValue);

            //subArray should have offsetStarts of MinValue and offsetEnds as MaxValue
            //so it will resize along with the parent
            Assert.IsTrue(view.OffsetStart.SequenceEqual(new[] {int.MinValue, int.MinValue}));
            Assert.IsTrue(view.OffsetEnd.SequenceEqual(new[] {int.MaxValue, int.MaxValue}));

            //assert values are equal
            Assert.AreEqual(array[0, 0], view[0, 0]);
            Assert.AreEqual(array[0, 1], view[0, 1]);
            Assert.AreEqual(array[1, 0], view[1, 0]);
            Assert.AreEqual(array[1, 1], view[1, 1]);
        }

        [Test]
        public void GenericFiltered()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(3);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;

            var view = array.Select(0, new[] {1});
            Assert.AreEqual(2, view[0]);
        }

        [Test]
        public void IndexOf()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(3);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;

            var view = array.Select(0, new[] { 1 });
            Assert.AreEqual(0, view.IndexOf(2));
        }


        [Test]
        [ExpectedException(typeof (IndexOutOfRangeException))]
        public void OutOfRangeForChildArray()
        {
            ///create a 2D grid and slice rows and columns
            /// 1 2
            /// 3 4
            IMultiDimensionalArray array = new MultiDimensionalArray(2, 2);
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 4;
            array[1, 1] = 5;

            //create a subarray of the top left corner
            // 1 |2|
            // 4 |5|
            //
            IMultiDimensionalArray subArray = array.Select(1, 1, 1);

            subArray[1, 1] = 5; // <= exception
        }


        [Test]
        public void ReduceArray()
        {
            // create a 2D grid and slice rows and columns
            // 1 2 3
            // 4 5 6
            // 7 8 9
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3);
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[0, 2] = 3;
            array[1, 0] = 4;
            array[1, 1] = 5;
            array[1, 2] = 6;
            array[2, 0] = 7;
            array[2, 1] = 8;
            array[2, 2] = 9;
            //create a reduced array containing the first row
            var view = array.Select(0, 0, 0);
            view.Reduce[0] = true; //reduce the x dimension
            Assert.AreEqual(1, view.Rank);
            Assert.AreEqual(new[] {3}, view.Shape);
            Assert.AreEqual(2, view[1]);
        }

        [Test]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ReduceOnWrongDimensionThrowsAnException()
        {
            var array = new MultiDimensionalArray(3, 3);
            var subArray = array.Select(0, 0, 1);
            subArray.Reduce[0] = true; //try to reduce the first dimension
        }

        [Test]
        public void RemoveAt()
        {
            //array = 1,2,3,4,5
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int> {1, 2, 3, 4, 5};
            //view  = 2,3
            IMultiDimensionalArrayView<int> view = new MultiDimensionalArrayView<int>(array, 0, 1, 2);
            Assert.IsTrue(new[] {2, 3}.SequenceEqual(view));
            view.RemoveAt(0);
            //array = 1,3,4,5
            //view  = 3
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(4, array.Count);
            Assert.IsTrue(new[] {3}.SequenceEqual(view));
            Assert.IsTrue(new[] {1, 3, 4, 5}.SequenceEqual(array));

            view.RemoveAt(0);
            Assert.AreEqual(0, view.Count);
            Assert.AreEqual(3, array.Count);
            Assert.IsTrue(new[] {1, 4, 5}.SequenceEqual(array));
        }

        [Test]
        public void ResizeParentAndUpdateShapeOfSubArray()
        {
            //create a 2D grid of 3x3
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3);

            //select rows [1, 2). So skip the first row
            IMultiDimensionalArray subArray = array.Select(0, 1, int.MaxValue);
            Assert.IsTrue(subArray.Shape.SequenceEqual(new[] {2, 3}));

            //resize the parent. Add one row and one column. Check the shape of the subArray changes
            array.Resize(new[] {4, 4});
            Assert.IsTrue(subArray.Shape.SequenceEqual(new[] {3, 4}));
        }

        [Test]
        public void Select()
        {
            // create a 2D grid and slice rows and columns
            //
            // 1 2 3
            // 4 5 6
            // 7 8 9
            //
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3);

            array[0, 0] = 1; // row 0
            array[0, 1] = 2;
            array[0, 2] = 3;

            array[1, 0] = 4; // row 1
            array[1, 1] = 5;
            array[1, 2] = 6;

            array[2, 0] = 7; // row 2
            array[2, 1] = 8;
            array[2, 2] = 9;
            Assert.AreEqual(9, array.Count);
            Assert.AreEqual(2, array[0, 1]);
            IMultiDimensionalArrayView subArray;

            // a new array is the middle column of our grid
            //
            // 1 [2] 3
            // 4 [5] 6
            // 7 [8] 9
            //    ^
            subArray = array.Select(new[] {int.MinValue, 1}, new[] {int.MaxValue, 1});

            Assert.AreEqual(3, subArray.Shape[0]);
            Assert.AreEqual(1, subArray.Shape[1]);
            Assert.AreEqual(int.MinValue, subArray.OffsetStart[0]);
            Assert.AreEqual(int.MaxValue, subArray.OffsetEnd[0]);
            Assert.AreEqual(1, subArray.OffsetStart[1]);
            Assert.AreEqual(1, subArray.OffsetEnd[1]);
            // check values
            Assert.AreEqual(2, subArray[0, 0]);
            Assert.AreEqual(5, subArray[1, 0]);
            Assert.AreEqual(8, subArray[2, 0]);

            // 1 [2] 3
            // 4 [5] 6
            // 7 [8] 9
            //    ^
            subArray = array.Select(1, 1, 1); // select 2st column, slicing

            Assert.AreEqual(3, subArray.Shape[0]);
            Assert.AreEqual(1, subArray.Shape[1]);
            Assert.AreEqual(int.MinValue, subArray.OffsetStart[0]);
            Assert.AreEqual(int.MaxValue, subArray.OffsetEnd[0]);
            Assert.AreEqual(1, subArray.OffsetStart[1]);
            Assert.AreEqual(1, subArray.OffsetEnd[1]);

            // 1 [2 3)
            // 4 [5 6)
            // 7 [8 9)
            //    ^

            subArray = array.Select(1, 1, int.MaxValue);

            Assert.AreEqual(3, subArray.Shape[0]);
            Assert.AreEqual(2, subArray.Shape[1]);
            Assert.AreEqual(int.MinValue, subArray.OffsetStart[0]);
            Assert.AreEqual(int.MaxValue, subArray.OffsetEnd[0]);

            Assert.AreEqual(1, subArray.OffsetStart[1]);
            Assert.AreEqual(int.MaxValue, subArray.OffsetEnd[1]);
        }

        [Test]
        public void SelectARow()
        {
            var array = new MultiDimensionalArray(2, 3);
            // 1 2 3     1 2 3
            // 4 5 6 ==> 
            
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[0, 2] = 3;
            array[1, 0] = 4;
            array[1, 1] = 5;
            array[1, 2] = 6;
            // select the first index of the first dimension (e.g. the row)
            var row =  array.Select(0,new[]{0});
            row.Reduce[0] = true;
            Assert.AreEqual(new[]{1,2,3},row);

        }
        [Test]
        public void SelectOnMultipleIndexes()
        {
            // more complex example (isn't it another test?)
            var array = new MultiDimensionalArray(3, 3);
            // 1 9 2     1   2
            // 9 9 9 ==> 
            // 3 9 4     3   4   
            array[0, 0] = 1;
            array[0, 1] = 9;
            array[0, 2] = 2;
            array[1, 0] = 9;
            array[1, 1] = 9;
            array[1, 2] = 9;
            array[2, 0] = 3;
            array[2, 1] = 9;
            array[2, 2] = 4;

            IMultiDimensionalArrayView view = array.Select(0, new[] {0, 2}).Select(1, new[] {0, 2});
            Assert.IsTrue(new[] {2, 2}.SequenceEqual(view.Shape));
            Assert.AreEqual(4, view.Count);
            Assert.AreEqual(1, view[0, 0]);
            Assert.AreEqual(2, view[0, 1]);
            Assert.AreEqual(3, view[1, 0]);
            Assert.AreEqual(4, view[1, 1]);
        }

        [Test]
        public void SelectUsingIndexes()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(3);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;

            IMultiDimensionalArrayView view = array.Select(0, new[] {0, 2});

            // Select first and last element
            Assert.AreEqual(2, view.Count);
            Assert.AreEqual(1, view[0]);
            Assert.AreEqual(3, view[1]);

            Assert.AreEqual(3,view.MaxValue);
            Assert.AreEqual(1,view.MinValue);
        }

        [Test]
        public void ShapeCalculationWithStartAndEndOffset()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3);

            //skip first element in both dimensions
            IMultiDimensionalArrayView subArray = new MultiDimensionalArrayView(array);
            subArray.OffsetStart[0] = 1;
            subArray.OffsetStart[1] = 1;
            Assert.IsTrue(subArray.Shape.SequenceEqual(new int[2] {2, 2}));

            //skip last element for both dimensions
            subArray = new MultiDimensionalArrayView(array);
            subArray.OffsetEnd[0] = 1;
            subArray.OffsetEnd[1] = 1;
            Assert.IsTrue(subArray.Shape.SequenceEqual(new int[2] {2, 2}));

            //skip last and first element for both dimensions
            subArray = new MultiDimensionalArrayView(array);
            subArray.OffsetEnd[0] = 1;
            subArray.OffsetEnd[1] = 1;
            subArray.OffsetStart[0] = 1;
            subArray.OffsetStart[1] = 1;
            Assert.IsTrue(subArray.Shape.SequenceEqual(new int[2] {1, 1}));

            //skip first element in both dimensions and resize parent
            subArray = new MultiDimensionalArrayView(array);
            subArray.OffsetStart[0] = 1;
            subArray.OffsetStart[1] = 1;
            //resize the parent array
            array.Resize(new[] {4, 4});
            Assert.IsTrue(subArray.Shape.SequenceEqual(new int[2] {3, 3}));
        }

        [Test]
        public void StrideCalculation()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(new[] {6, 1});

            IMultiDimensionalArray array2 = new MultiDimensionalArray(new[] {6, 3});
            //reduce the 2nd dimension to only the 1st element. So it resembles the array
            IMultiDimensionalArray view = new MultiDimensionalArrayView(array2, 1, 0, 0);
            Assert.AreEqual(array.Stride, view.Stride);
        }

        [Test]
        public void ToString()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int> {1, 2, 3, 4, 5};
            IMultiDimensionalArrayView view = new MultiDimensionalArrayView(array, 0, 1, 2);
            string s = view.ToString();

            Assert.IsTrue(s.Contains(2.ToString()));
            Assert.IsTrue(s.Contains(3.ToString()));

            Assert.IsFalse(s.Contains(1.ToString()));
            Assert.IsFalse(s.Contains(4.ToString()));
            Assert.IsFalse(s.Contains(5.ToString()));
        }

        [Test]
        public void UseEnumeratorOnMultiDimensionalArrayView()
        {
            // Setup an array
            // 1 2 -
            // 3 4 - 
            // - - - 
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(3, 3);
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 3;
            array[1, 1] = 4;

            // make a selection of the top right corner
            IMultiDimensionalArray<double> view = array.Select(0, 0, 1).Select(1, 0, 1);
            Assert.IsTrue(new[] {2, 2}.SequenceEqual(view.Shape));

            // since array supports enumerator - we can enumerate throuth all values as 1D array
            Assert.IsTrue(new double[] {1, 2, 3, 4}.SequenceEqual(view));
        }

        [Test]
        public void MoveDimensionAtGivenIndexAndLength()
        {
            var values = new List<int> { 1, 2, 3, 4 };
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(values, new[] { 1, 4 });

            var view = array.Select(1, 2, 3); // select { 1, 2, [3, 4] }

            // move 2nd dimension index
            var dimension = 1;
            var startIndex = 1;
            var length = 2;
            var newIndex = 0;
            array.Move(dimension, startIndex, length, newIndex); // 1, 2, 3, 4  => 2, 3, 1, 4

            //  2, 3, [1, 4]
            Assert.IsTrue(view.SequenceEqual(new[] { 1, 4 }));
        }

        [Test]
        public void Clone()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(3);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;

            var view = array.Select(0, 1, 1);

            var clonedView = (IMultiDimensionalArrayView)view.Clone();

            Assert.IsTrue(view.OffsetStart.SequenceEqual(clonedView.OffsetStart));
            Assert.IsTrue(view.OffsetEnd.SequenceEqual(clonedView.OffsetEnd));
        }
    }
}