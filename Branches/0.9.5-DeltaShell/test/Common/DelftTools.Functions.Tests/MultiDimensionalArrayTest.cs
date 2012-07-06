using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions.Generic;
using DelftTools.Functions.Tests.TestObjects;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Reflection;
using log4net;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class MultiDimensionalArrayTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MultiDimensionalArrayTest));

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
        public void ChangeSizeOfFirstDimension()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();

            array.Resize(1, 2);

            array[0, 0] = 1; // 0 + 0
            array[0, 1] = 2; // 0 + 1
            // 2   1 <= stride

            log.InfoFormat("Before resize: {0}", array.ToString());

            array.Resize(new[] { 2, 2 });

            log.InfoFormat("After resize: {0}", array.ToString());

            Assert.AreEqual(2, array.Rank);
            Assert.AreEqual(4, array.Count);

            Assert.AreEqual(1, array[0, 0]);
            Assert.AreEqual(2, array[0, 1]);
            Assert.IsNull(array[1, 0]);
            Assert.IsNull(array[1, 1]);
        }

        [Test]
        public void ChangeSizeOfLastDimension()
        {
            //we don't use interface because want to check stride
            var array = new MultiDimensionalArray();

            array.Resize(2, 1);

            array[0, 0] = 1; // 0 + 0
            array[1, 0] = 2; // 2 + 0

            // 1   1 <= stride
            Assert.AreEqual(new[] { 1, 1 }, array.Stride);

            log.InfoFormat("Before resize: {0}", array.ToString());

            array.Resize(new[] { 2, 2 });

            log.InfoFormat("After resize: {0}", array.ToString());

            Assert.AreEqual(2, array.Rank);
            Assert.AreEqual(4, array.Count);

            Assert.AreEqual(1, array[0, 0]);
            Assert.AreEqual(2, array[1, 0]);
            Assert.IsNull(array[0, 1]);
            Assert.IsNull(array[1, 1]);
        }

        [Test]
        public void Clear()
        {
            IList<int> values = new List<int> { 1, 2, 3, 4 };

            var array = new MultiDimensionalArray<int>(values, new[] { 1, 4 });
            array.Clear();

            Assert.AreEqual(0, array.Count);
            Assert.AreEqual(new[] { 0, 0 }, array.Shape);
        }

        [Test]
        public void Clear2()
        {
            IList<int> values = new List<int> { 1, 2, 3, 4 };

            var array = new MultiDimensionalArray<int>(values, new[] { 4 });
            array.Clear();

            Assert.AreEqual(0, array.Count);
            Assert.AreEqual(new[] { 0 }, array.Shape);
        }


        [Test]
        public void CollectionChangeEvents_Enlarge()
        {
            //enlarge an array
            IMultiDimensionalArray array = new MultiDimensionalArray(1);
            int collectionChangingEventCount = 0;
            int collectionChangedEventCount = 0;
            array.CollectionChanging += delegate { collectionChangingEventCount++; };
            array.CollectionChanged += delegate { collectionChangedEventCount++; };

            array.Resize(2, 2);

            Assert.AreEqual(3, collectionChangingEventCount);
            Assert.AreEqual(3, collectionChangedEventCount);
        }

        [Test]
        public void CollectionChangeEvents_Enlarge2()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(2, 2);
            int collectionChangingEventCount = 0;
            int collectionChangedEventCount = 0;
            array.CollectionChanging += delegate { collectionChangingEventCount++; };
            array.CollectionChanged += delegate { collectionChangedEventCount++; };

            array.Resize(2, 3);

            Assert.AreEqual(2, collectionChangingEventCount);
            Assert.AreEqual(2, collectionChangedEventCount);
        }

        [Test]
        public void CollectionChangedEventArgsIndexes()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(1, 2, 2);

            int callCount = 0;
            array.CollectionChanged += delegate(object sender, MultiDimensionalArrayChangingEventArgs e)
                                           {
                                               switch (callCount++)
                                               {
                                                   case 0: Assert.IsTrue(new[] { 1, 0, 0 }.SequenceEqual(e.MultiDimensionalIndex));
                                                       break;
                                                   case 1: Assert.IsTrue(new[] { 1, 0, 1 }.SequenceEqual(e.MultiDimensionalIndex));
                                                       break;
                                                   case 2: Assert.IsTrue(new[] { 1, 1, 0 }.SequenceEqual(e.MultiDimensionalIndex));
                                                       break;
                                                   case 3: Assert.IsTrue(new[] { 1, 1, 1 }.SequenceEqual(e.MultiDimensionalIndex));
                                                       break;
                                               }
                                           };

            array.Resize(new[] { 2, 2, 2 });

        }

        [Test]
        public void CollectionChangeEvents_Shrink()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(2, 2);

            int collectionChangingEventCount = 0;
            int collectionChangedEventCount = 0;
            array.CollectionChanging += delegate { collectionChangingEventCount++; };
            array.CollectionChanged += delegate { collectionChangedEventCount++; };

            array.Resize(1);

            Assert.AreEqual(3, collectionChangingEventCount);
            Assert.AreEqual(3, collectionChangedEventCount);
        }

        [Test]
        public void Contains()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            array.Add(22);
            //array.SetValues(new[] {1,2,22});
            Assert.IsTrue(array.Contains(22));
            Assert.IsFalse(array.Contains(23));
        }

        [Test]
        public void ConvertToListUsingCopyConstructor()
        {
            IList<int> values = new List<int> { 1, 2, 3, 4 };

            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(values, new[] { 1, 4 });

            IList array1D = new List<int>(array);
            Assert.AreEqual(4, array1D.Count);
            Assert.AreEqual(4, array1D[3]);
            Assert.AreEqual(2, array1D[1]);
        }

        [Test]
        public void CopyConstructor()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            array.Add(1);
            array.Add(2);
            array.Add(3);

            Assert.AreEqual(3, array.Shape[0]);
            //create an offline copy
            IMultiDimensionalArray<int> copy = new MultiDimensionalArray<int>(array, array.Shape);
            Assert.AreEqual(3, copy.Count);

            //add an item to the parent and verify it does not add to our copy
            array.Add(5);
            Assert.AreEqual(3, copy.Count);
        }

        [Test]
        public void CopyConstructor1D()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2, 2);
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 3;
            array[1, 1] = 4;


            // copy 2D array to 1D list
            IList<int> list = new List<int>(array);
            Assert.AreEqual(4, list.Count, "number of elements copied from multi-dimensional array is wrong");

            // creates a new MD array. since no shape is available will assume 1D
            IMultiDimensionalArray<int> copy = new MultiDimensionalArray<int>(list);

            Assert.IsTrue(new[] { 4 }.SequenceEqual(copy.Shape), "shape of the multi-dimensional array is incorrect");
            Assert.AreEqual(array[1, 1], copy[3]);
        }

        [Test]
        public void CopyConstructor2D()
        {
            // 2 d version
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2, 2);
            // 1 2
            // 3 4
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 3;
            array[1, 1] = 4;

            //copy constructor with MDA should copy shape.
            IMultiDimensionalArray<int> copy = new MultiDimensionalArray<int>(array);
            Assert.IsTrue(new[] { 2, 2 }.SequenceEqual(copy.Shape));
            Assert.AreEqual(array[0, 1], copy[0, 1]);
        }

        [Test]
        public void Create1D()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();

            array.Add(1);
            array.Add(2);
            array.Add(3);
            array.Remove(2);

            log.Info(array.ToString());

            Assert.AreEqual(1, array.Rank);
            Assert.AreEqual(2, array.Count);
        }

        [Test]
        public void Create3D()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(2, 2, 2);

            array[0, 0, 0] = 1; // 0 + 0 + 0
            array[0, 0, 1] = 2; // 0 + 0 + 1
            array[0, 1, 0] = 3; // 0 + 4 + 0 
            array[0, 1, 1] = 4; // 0 + 4 + 1
            array[1, 0, 0] = 5; // 4 + 0 + 0
            array[1, 0, 1] = 6; // 4 + 0 + 0
            array[1, 1, 0] = 7; // 4 + 2 + 0
            array[1, 1, 1] = 8; // 4 + 2 + 2
            // 4   2   1 <= strides

            log.InfoFormat("3D array: {0}", array.ToString());

            Assert.AreEqual(3, array.Rank);
            Assert.AreEqual(8, array.Count);
        }

        [Test]
        public void DecreaseNumberOfDimensions()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();
            array.Resize(2, 2);
            array[0, 0] = 5;
            array[1, 1] = 2;

            //drop one dimensions
            array.Resize(new[] { 2 });

            //now we expect the values to be taken from dimidx 0 of the removed dimension
            Assert.AreEqual(5, array[0]);
            Assert.AreEqual(null, array[1]);
        }

        [Test]
        public void DefaultValueForReferenceTypes()
        {
            IMultiDimensionalArray<string> array = new MultiDimensionalArray<string> { DefaultValue = "dummy" };
            array.Resize(2, 2);
            Assert.AreEqual("dummy", array[1, 1]);
        }

        [Test]
        public void DefaultValueForValueTypes()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int> { DefaultValue = 99 };
            array.Resize(2, 2);
            Assert.AreEqual(99, array[1, 1]);
        }



        [Test]
        public void IncreaseNumberOfDimensions()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();
            array.Resize(2, 2);
            array[0, 0] = 5;
            array[1, 1] = 2;
            //add an extra dimension
            array.Resize(new[] { 2, 2, 4 });
            //now we expect the values to be set for the index 0 of this 3rd dimension
            Assert.AreEqual(5, array[0, 0, 0]);
            Assert.AreEqual(null, array[0, 0, 1]);
            Assert.AreEqual(2, array[1, 1, 0]);
            Assert.AreEqual(null, array[1, 1, 1]);
        }

        [Test]
        public void IndexOnValuesAdded()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            int callCount = 0;
            array.Add(2);
            array.CollectionChanged += delegate(object sender, MultiDimensionalArrayChangingEventArgs e)
                                           {
                                               Assert.AreEqual(new[] { 1 }, e.MultiDimensionalIndex);
                                               callCount++;
                                           };
            array.Add(4);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void IndexOnValuesChanged()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2, 2);
            int callCount = 0;
            array.CollectionChanged += delegate(object sender, MultiDimensionalArrayChangingEventArgs e)
                                           {
                                               Assert.AreEqual(new[] { 1, 1 }, e.MultiDimensionalIndex);
                                               callCount++;
                                           };
            array[1, 1] = 20;
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void IndexOnValuesRemoved()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            array.Add(2);
            array.Add(3);
            int callCount = 0;
            array.CollectionChanged += (sender, e) =>
                                           {
                                               Assert.AreEqual(new[] {1}, e.MultiDimensionalIndex);
                                               callCount++;
                                           };
            array.Remove(3);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void InsertAndRemoveOneDimensionalArray()
        {
            //{ 1, 3, 4} --> {1, 2, 3, 4}
            IMultiDimensionalArray array = new MultiDimensionalArray(3);
            array[0] = 1;
            array[1] = 3;
            array[2] = 4;

            //insert our new item
            array.InsertAt(0, 1);
            array[1] = 2;
            //TODO : find a nicer way for these asserts.
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            Assert.AreEqual(3, array[2]);
            Assert.AreEqual(4, array[3]);

            // {1,2,3,4} --> {1,4}
            array.RemoveAt(0, 1, 2);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(4, array[1]);
            Assert.AreEqual(2, array.Count);
        }

        [Test]
        public void InsertAt()
        {
            // 1 2
            // 5 6 
            IMultiDimensionalArray array = new MultiDimensionalArray(2, 2);
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 5;
            array[1, 1] = 6;
            Assert.IsTrue(array.Shape.SequenceEqual(new[] { 2, 2 }));
            Assert.IsTrue(array.Stride.SequenceEqual(new[] { 2, 1 }));

            //insert a row between the other rows
            array.InsertAt(0, 1);
            array[1, 0] = 3;
            array[1, 1] = 4;
            //now should have the following
            // 1 2
            // 3 4
            // 5 6
            Assert.IsTrue(array.Shape.SequenceEqual(new[] { 3, 2 }));
            Assert.IsTrue(array.Stride.SequenceEqual(new[] { 2, 1 }));
            Assert.AreEqual(2, array[0, 1]);
            Assert.AreEqual(3, array[1, 0]);
            Assert.AreEqual(4, array[1, 1]);
            Assert.AreEqual(6, array[2, 1]);

            //now insert two empty columns like this
            // - - 1 2
            // - - 3 4
            // - - 5 6 
            array.InsertAt(1, 0, 2);
            Assert.IsTrue(array.Shape.SequenceEqual(new[] { 3, 4 }));
            Assert.IsTrue(array.Stride.SequenceEqual(new[] { 4, 1 }));
            Assert.AreEqual(null, array[0, 0]);
            Assert.AreEqual(null, array[0, 1]);
            Assert.AreEqual(1, array[0, 2]);
            Assert.AreEqual(2, array[0, 3]);
        }

        [Test]
        public void InsertAt_3D()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(2, 2, 2);
            array[0, 0, 0] = 1;
            array[0, 0, 1] = 5;
            array[0, 1, 0] = 2;
            array[0, 1, 1] = 6;
            array[1, 0, 0] = 3;
            array[1, 0, 1] = 7;
            array[1, 1, 0] = 4;
            array[1, 1, 1] = 8;
            /*
             {
               {{1, 5}{2, 6}}
               {{3, 7}{4, 8}}
             }
             */

            Assert.IsTrue(array.Shape.SequenceEqual(new[] { 2, 2, 2 }));
            Assert.IsTrue(array.Stride.SequenceEqual(new[] { 4, 2, 1 }));

            array.InsertAt(2, 0);
            /*
             {
               {{<null>, 1, 5}{<null>, 2, 6}}
               {{<null>, 3, 7}{<null>, 4, 8}}
             }
             */

            Assert.IsTrue(array.Shape.SequenceEqual(new[] { 2, 2, 3 }));
            Assert.IsTrue(array.Stride.SequenceEqual(new[] { 6, 3, 1 }));

            array.InsertAt(2, 3);
            /*
             {
               {{<null>, 1, 5, <null>}{<null>, 2, 6, <null>}}
               {{<null>, 3, 7, <null>}{<null>, 4, 8, <null>}}
             }
             */

            Assert.IsTrue(array.Shape.SequenceEqual(new[] { 2, 2, 4 }));
            Assert.IsTrue(array.Stride.SequenceEqual(new[] { 8, 4, 1 }));
        }

        [Test]
        public void InsertAtRemoveAt()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();
            array.Resize(2, 2);
            array[0, 0] = 5;
            array[1, 1] = 2;

            array.InsertAt(0, 2);
            log.Info(array.ToString());
            array.RemoveAt(0, 2);
            log.Info(array.ToString());
            array.InsertAt(0, 2);
            log.Info(array.ToString());
            array.RemoveAt(0, 2);
            log.Info(array.ToString());
        }

        [Test]
        public void InsertOneRowInMatrix()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidNumberOfIndexesException()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray(3, 3);
            //try to use wrong index for accessing
            array[0, 0, 0] = 5;
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException))]
        public void AddWrongTypeToValuesGivesException()
        {
            IMultiDimensionalArray<DateTime> array = new MultiDimensionalArray<DateTime>();
            array.Add(1); // <-- exception
        }

        [Test]
        public void AddEmptyDimensionToArray()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(2, 2);
            array.Resize(2, 2, 0);
            Assert.AreEqual(0, array.Count);
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException))]
        public void SetValuesOfAWrongTypeGivesException()
        {
            IMultiDimensionalArray<DateTime> array = new MultiDimensionalArray<DateTime>();
            array.Add(DateTime.Now);
            ((IList)array)[0] = 1; // <-- exception
        }

        [Test]
        public void LastValue()
        {
            //last does not work because the single dimensional indexing does not work see test above
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>();
            array.Add(3.0);
            Assert.AreEqual(1, array.Count);
            Assert.AreEqual(3.0, array.Last());
        }

        [Test]
        public void NotifyCollectionChanged()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2, 2);
            bool called = false;
            array.CollectionChanged += delegate { called = true; };
            array[0, 0] = 0;
            array[0, 1] = 1;
            array[1, 0] = 2;
            array[1, 1] = 3;
            //called when values added
            Assert.IsTrue(called);

            //called when we replace a value
            called = false;
            array[0, 0] = 5;
            Assert.IsTrue(called);
        }

        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        [NUnit.Framework.Category(TestCategory.WorkInProgress)] // slow
        public void PerformanceChangeSizeOfFirstDimensions_WithEvents()
        {
            const int valuesToAdd = 100000;

            Action action = delegate
                                {

                                    // increasing the 1st dimension should be fast. 
                                    // For example when we add a new measurement for a 3x3 grid
                                    IMultiDimensionalArray array = new MultiDimensionalArray(0, 5, 5);
                                    array.CollectionChanged += delegate { };

                                    //add more data throught and increase the 1st dimenion-size
                                    for (int i = 0; i < valuesToAdd; i++)
                                    {
                                        array.Resize(new[] {i + 1, 5, 5});
                                        array[i, 1, 1] = 5; //everywhere a five.
                                    }
                                };

            TestHelper.AssertIsFasterThan(550, "Added" + valuesToAdd + " values", action);
        }

        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        [NUnit.Framework.Category(TestCategory.WorkInProgress)] // slow
        public void PerformanceChangeSizeOfFirstDimensions_WithoutEvents()
        {
            // increasing the 1st dimension should be fast. 
            // For example when we add a new measurement for a 3x3 grid
            IMultiDimensionalArray array = new MultiDimensionalArray<int>(0, 5, 5);

            array.FireEvents = false;
            //add more data throught and increase the 1st dimenion-size
            const int valuesToAdd = 100000;

            Action action = delegate
                                {
                                    for (int i = 0; i < valuesToAdd; i++)
                                    {
                                        array.Resize(new[] {i + 1, 5, 5});
                                        array[i, 1, 1] = 5; //everywhere a five.
                                    }
                                };

            TestHelper.AssertIsFasterThan(325, action);

            for (int i = 0; i < valuesToAdd - 1; i++)
            {
                Assert.AreEqual(5, array[i, 1, 1]);
            }
        }

        [Test]
        public void RemoveAt()
        {
            // Strip an array of 2 column and 2 rows
            // 1 9 9 2      1 2 
            // 9 9 9 9 ==>  3 4
            // 3 9 9 4      5 6       
            // 5 9 9 6
            // setup this array
            IMultiDimensionalArray array = new MultiDimensionalArray(4, 4);
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    array[i, j] = 9;
            array[0, 0] = 1;
            array[0, 3] = 2;
            array[2, 0] = 3;
            array[2, 3] = 4;
            array[3, 0] = 5;
            array[3, 3] = 6;

            //remove
            array.RemoveAt(0, 1); //remove one row
            array.RemoveAt(1, 1, 2); //remove two columns

            //test results
            Assert.AreEqual(6, array.Count);
            Assert.AreEqual(1, array[0, 0]);
            Assert.AreEqual(2, array[0, 1]);
            Assert.AreEqual(3, array[1, 0]);
            Assert.AreEqual(4, array[1, 1]);
            Assert.AreEqual(5, array[2, 0]);
            Assert.AreEqual(6, array[2, 1]);
        }

        [Test]
        public void RemoveAt_1D()
        {
            //{ 1, 3, 4} --> {1,4}
            IMultiDimensionalArray array = new MultiDimensionalArray(3);
            array[0] = 1;
            array[1] = 3;
            array[2] = 4;

            array.RemoveAt(1);

            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(4, array[1]);
        }


        [Test]
        public void ResizeArrayResizesInternalValues()
        {
            //reduce size by 1
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2);
            array[0] = 2;
            array[1] = 3;
            array.Resize(1);
            var values = TypeUtils.GetField<MultiDimensionalArray, IList>(array, "values");
            Assert.AreEqual(1, values.Count);

            //clear the whole thing
            array = new MultiDimensionalArray<int>(2);
            array[0] = 2;
            array[1] = 3;
            array.Resize(0);
            values = TypeUtils.GetField<MultiDimensionalArray, IList>(array, "values");
            Assert.AreEqual(0, values.Count);
        }

        [Test]
        public void ResizeFirstDimensionValuesChanged()
        {
            IMultiDimensionalArray<int> multiDimensionalArray = new MultiDimensionalArray<int>(1);
            int callCount = 0;
            multiDimensionalArray.CollectionChanged +=
                delegate { callCount++; };
            multiDimensionalArray.Resize(2);
            Assert.AreEqual(1, callCount);

            //resize a more dimensional array
            multiDimensionalArray = new MultiDimensionalArray<int>(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new[] { 2, 5 });
            multiDimensionalArray.CollectionChanged +=
                delegate (object sender, MultiDimensionalArrayChangingEventArgs args)
                    {
                        callCount++;
                        Assert.AreEqual(5, args.Items.Count);
                    };
            callCount = 0;

            //generate two change events
            multiDimensionalArray.Resize(3, 5);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void StrideCalculation()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();

            array.Resize(1, 2);
            Assert.AreEqual(new[] { 2, 1 }, array.Stride);
        }

        [Test]
        public void StrideCalculation2()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray();
            // create grid 2 rows and 6 colums
            array.Resize(2, 6);
            // cost to move 1 column, 1 row
            Assert.AreEqual(new[] { 6, 1 }, array.Stride);
        }

        [Test]
        public void TestRank()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(2, 2);
            Assert.AreEqual(2, array.Rank);
        }

        [Test]
        public void UserOneDimensionalListInterfaceOnMultiDimensionalArray()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(2, 2);

            array[0, 0] = 5;
            IList<double> list = array;
            Assert.AreEqual(5, list[0]);
        }

        [Test]
        public void ValuesConstructor()
        {

            IList<int> values = new List<int> { 1, 2, 3, 4 };

            //first dimension has 1 element 2nd has 4
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>(values, new[] { 1, 4 });
            Assert.AreEqual(4, array[0, 3]);

            //first dimension has 4 elements 2nd has 1 
            array = new MultiDimensionalArray<int>(values, new[] { 4, 1 });
            Assert.AreEqual(4, array[3, 0]);

            //2x2
            array = new MultiDimensionalArray<int>(values, new[] { 2, 2 });
            Assert.AreEqual(4, array[1, 1]);

            //3x2
            values = new List<int> { 1, 2, 3, 4, 5, 6 };
            array = new MultiDimensionalArray<int>(values, new[] { 2, 3 });
            Assert.AreEqual(4, array[1, 0]);
        }

        

        [Test]
        public void MinAndMaxValue()
        {
            IMultiDimensionalArray array = new MultiDimensionalArray<int>();
            array.AddRange(new[] { 1, 2, 3 });
            Assert.AreEqual(1, array.MinValue);
            Assert.AreEqual(3, array.MaxValue);

            array = new MultiDimensionalArray<double>();
            array.AddRange(new[] { 1.0d, 2.0d, 3.0d });
            Assert.AreEqual(1.0d, array.MinValue);
            Assert.AreEqual(3.0d, array.MaxValue);

            array = new MultiDimensionalArray<float>();
            array.AddRange(new[] { 1.0f, 2.0f, 3.0f });
            Assert.AreEqual(1.0f, array.MinValue);
            Assert.AreEqual(3.0f, array.MaxValue);

            array = new MultiDimensionalArray(2, 2);
            array[0, 0] = -2;
            array[0, 1] = 5;
            Assert.AreEqual(-2, array.MinValue);
            Assert.AreEqual(5, array.MaxValue);
        }

        [Test]
        public void Resize()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            array.Resize(new[] { 3 });
        }

        [Test]
        public void ResizeFirstDimension()
        {
            IMultiDimensionalArray<int> array = new MultiDimensionalArray<int>();
            array.Resize(1);
        }

        [Test]
        public void MoveDimensionAtGivenIndexAndLength()
        {
            var values = new List<int> { 1, 2, 3, 4, 5 };
            var array = new MultiDimensionalArray<int>(values, new[] {1, 5 });

            // move 2nd dimension index
            var dimension = 1;
            var startIndex = 1;
            var length = 2;
            var newIndex = 3;

            array.Move(dimension, startIndex, length, newIndex);

            Assert.IsTrue(array.SequenceEqual(new[] { 1, 4, 5, 2, 3 }));
        }

        [Test]
        public void MoveDimensionToBeginAtGivenIndexAndLength()
        {
            var values = new List<int> { 1, 2, 3, 4, 5 };
            var array = new MultiDimensionalArray<int>(values, new[] { 5 });

            // move 2nd dimension index
            var dimension = 0;
            var startIndex = 2;
            var length = 2;
            var newIndex = 0;


            int callCount = 0;
            array.CollectionChanged += delegate { callCount++; };
            array.Move(dimension, startIndex, length, newIndex);

            Assert.IsTrue(array.SequenceEqual(new[] { 3, 4, 1, 2, 5 }));
            Assert.AreEqual(4,callCount);
        }
        [Test]
        public void MoveSingleValueToEnd()
        {
            var values = new List<int> { 20, 30, 50 };
            var array = new MultiDimensionalArray<int>(values, new[] { 3});

            // move 2nd dimension index
            var dimension = 0;
            var startIndex = 0;
            var length = 1;
            var newIndex = 2;

            int callCount = 0;
            array.CollectionChanged += delegate { callCount++; };
            array.Move(dimension, startIndex, length, newIndex);

            Assert.AreEqual(new[] { 30, 50,20},array);
            Assert.AreEqual(3,callCount);
        }


        [Test]
        public void MoveValuesToStart()
        {
            var values = new List<int> {1, 2, 3, 4, 5};
            var array = new MultiDimensionalArray<int>(values, new[] {5});

            // move 2nd dimension index
            var dimension = 0;
            var startIndex = 3;
            var length = 2;
            var newIndex = 0;

            int collectionChangedCallCount = 0;
            int collectionChangingCallCount = 0;
            array.CollectionChanged += (s,e)=>
                                           {
                                               collectionChangedCallCount++;
                                           };

            array.CollectionChanging += (s, e) =>
                                            {
                                                //TODO add check for values..
                                                collectionChangingCallCount++;
                                            };
            
            array.Move(dimension, startIndex, length, newIndex);
            
            Assert.AreEqual(new[] {4, 5, 1, 2, 3}, array);
            //every value changed
            Assert.AreEqual(5,collectionChangedCallCount);
            Assert.AreEqual(5, collectionChangingCallCount);
            
        }

        [Test]
        public void InitializeElementsFromMultiDimensionalArray()
        {
            var values = new[,]
                             {
                                 {1, 2, 3},
                                 {4, 5, 6}
                             };
            var array = new MultiDimensionalArray<int>(values, new[] { 2, 3 });
            Assert.IsTrue(array.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void Parse1DFromString()
        {
            var array = MultiDimensionalArray<int>.Parse("{0, 1, 2}");

            array.Rank
                .Should("array rank").Be.EqualTo(1);

            array.Shape
                .Should("have shape of 3").Have.SameSequenceAs(new[] { 3 });

            array
                .Should("check element values").Have.SameSequenceAs(new[] {0, 1, 2});
        }

        [Test]
        public void Parse2DFromString()
        {
            var array = MultiDimensionalArray<int>.Parse("{{0, 1, 2}, {3, 4, 5}}");

            array.Rank
                .Should("array rank").Be.EqualTo(2);
            
            array.Shape
                .Should("have shape of 2x2").Have.SameSequenceAs(new[] {2, 3});
            
            array[1, 2]
                .Should("check last element").Be.EqualTo(5);
        }

        [Test]
        public void CloneGeneric()
        {
            var array = new MultiDimensionalArray<int>(new[] { 1, 2, 3, 4 }, new[] { 2, 2 });
            
            var clone = (IMultiDimensionalArray<int>)array.Clone();

            clone.Rank
                .Should().Be.EqualTo(2);

            clone.Shape
                .Should().Have.SameSequenceAs(new[] { 2, 2 });

            clone
                .Should().Have.SameSequenceAs(array);
        }

        [Test]
        public void Clone2()
        {
            var array = (IMultiDimensionalArray)new MultiDimensionalArray<int>(new[] { 1, 2, 3, 4 }, new[] { 2, 2 });

            var clone = (IMultiDimensionalArray<int>)array.Clone();

            clone.Rank
                .Should().Be.EqualTo(2);

            clone.Shape
                .Should().Have.SameSequenceAs(new[] { 2, 2 });
        }

        [Test]
        public void Clone3()
        {
            var array = (MultiDimensionalArray)new MultiDimensionalArray<int>(new[] { 1, 2, 3, 4 }, new[] { 2, 2 });

            var clone = (IMultiDimensionalArray<int>)array.Clone();

            clone.Rank
                .Should().Be.EqualTo(2);

            clone.Shape
                .Should().Have.SameSequenceAs(new[] { 2, 2 });
        }

        [Test]
        public void UpdateMinMax()
        {
            var array = new MultiDimensionalArray<double>();
            Assert.IsNull(array.MinValue);
            Assert.IsNull(array.MaxValue);

            //action! change the array
            array.Add(0.0d);

            //assert min max got updated
            Assert.AreEqual(0.0d,array.MinValue);
            Assert.AreEqual(0.0d, array.MaxValue);

            //make sure we got insert covered
            array.Insert(1,1.0d);
            Assert.AreEqual(1.0d, array.MaxValue);
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Performance)]
        [NUnit.Framework.Category(TestCategory.BadQuality)] // TODO: split this test in two tests measuring actual time and use TestHelper.AssertIsFasterThan()
        [NUnit.Framework.Category(TestCategory.WorkInProgress)] // slow
        public void InsertAtOnFirstDimensionShouldPerformLinearly()
        {
            //TODO: move this test back to MDAT...only here to use functionview..

            // TODO: split this test in two tests measuring actual time and use TestHelper.AssertIsFasterThan()

            int secondDimensionSize = 500;
            var array = new MultiDimensionalArray(new[] { 0, secondDimensionSize });
            int collectionChangedCount = 0;
            array.CollectionChanged += (s, e) =>
            {
                collectionChangedCount++;
            };
            var perfSeries = new Function { Arguments = { new Variable<int> { Name = "values added" } }, Components = { new Variable<double> { Name = "total ticks" } } };

            var startTime = DateTime.Now;
            int slicesToAdd = 20000;
            for (int i = 0; i < slicesToAdd; i++)
            {
                array.InsertAt(0, i);
                if ((i % 1000) == 0)
                {
                    perfSeries[i] = (DateTime.Now - startTime).TotalMilliseconds;
                }
            }
            //one collection change per slice
            Assert.AreEqual(slicesToAdd, collectionChangedCount);
            //check the numberslice/elapsed is just about constant..
            var ratios = new List<double>();
            for (int i = 0; i < perfSeries.Arguments[0].Values.Count; i++)
            {
                ratios.Add(((double)perfSeries.Components[0].Values[i]) / (int)perfSeries.Arguments[0].Values[i]);
            }
            //check the ratios are 'about' the same 'everywhere'..get this formalized in a testhelper or something..
            Assert.AreEqual(ratios[5], ratios[9], 0.05);
            Assert.AreEqual(ratios[12], ratios[9], 0.05);
            Assert.AreEqual(ratios[12], ratios[4], 0.05);
            // to see it move this test to functionviewtest and uncomment
            //var functionView = new FunctionView { Data = perfSeries };
            //WindowsFormsTestHelper.ShowModal(functionView);
        }
        
        [Test]
        [ExpectedException(typeof(InvalidOperationException),ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyAdd()
        {
            var array = new MultiDimensionalArray(true, false, 1, new int[1]);
            array.Add(1);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyRemove()
        {
            var array = new MultiDimensionalArray(true, false, 1, new int[1]);
            array.Remove(1);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyIndexer()
        {
            var array = new MultiDimensionalArray(true, false, 1, new int[1]);
            array[0] = 1;
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyClear()
        {
            var array = new MultiDimensionalArray(true, false, 1, new int[1]);
            array.Clear();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyInsert()
        {
            var array = new MultiDimensionalArray(true, false, 1, new int[1]);
            array.Insert(0, 2);
        }

        [Test]
        public void ReadOnlyRemoveAt()
        {
            int exCount = 0;
            var array = new MultiDimensionalArray(true, false, 1,new[]{1}, new[]{1});
            IList<Action> calls = new List<Action>
                                      {
                                          () => array.RemoveAt(0, 0, 1), 
                                          () => array.RemoveAt(0, 0),
                                          () => array.RemoveAt(0)
                                      };
            calls.ForEach((a) =>
                              {
                                  try
                                  {
                                      a();
                                  }
                                  catch (Exception ex)
                                  {
                                      Assert.IsTrue(ex is InvalidOperationException);
                                      Assert.AreEqual("Illegal attempt to modify readonly array", ex.Message);
                                      exCount++;
                                  }
                              });
            Assert.AreEqual(3,exCount);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyResize()
        {
            var array = new MultiDimensionalArray(true, false, 1, new[] { 1 }, new[] { 1 });
            array.Resize(new[]{2,2});
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyInsertAt()
        {
            var array = new MultiDimensionalArray(true, false, 1, new[] { 1 }, new[] { 1 });
            array.InsertAt(0,0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyInsertAt2Args()
        {
            var array = new MultiDimensionalArray(true, false, 1, new[] { 1 }, new[] { 1 });
            array.InsertAt(0, 0,1);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Illegal attempt to modify readonly array")]
        public void ReadOnlyMove()
        {
            var array = new MultiDimensionalArray(true, false, 1, new[] { 1 }, new[] { 1 });
            array.Move(0, 0, 0,0);
        }

        [Test]
        public void EmptyFirstDimensionDoesNotAlterSecondDimension()
        {
            var array = new MultiDimensionalArray<double>(new[] {1, 1});
            array.RemoveAt(0,0);
            Assert.AreEqual(new[]{0,1},array.Shape);
        }

        [Test]
        public void InsertAtWithValuesAtTheEnd()
        {
            var array = new MultiDimensionalArray<int>(new[] {1});
            array[0] = 4;
            array.InsertAt(0, 1, 3, new object[] {1, 2, 3});
            Assert.AreEqual(new[] {4}, array.Shape);
            Assert.AreEqual(new[] {4, 1, 2, 3}, array);
        }

        [Test]
        public void InsertAtWithValuesInTheMiddle()
        {
            var array = new MultiDimensionalArray<int>(new[] {3});
            array[0] = 0;
            array[1] = 4;
            array[2] = 5;
            array.InsertAt(0, 1, 3, new object[] {1, 2, 3});
            Assert.AreEqual(new[] {6}, array.Shape);
            Assert.AreEqual(new[] {0, 1, 2, 3, 4,5}, array);
        }


        [Test]
        public void InsertAtGivesAggregatedCollectionChangingEvent()
        {
            var array = new MultiDimensionalArray<double>(new[] {2, 2});

            var callCount = 0;
            array.CollectionChanging += (s, e) =>
                                            {
                                                callCount++;
                                                Assert.AreEqual(NotifyCollectionChangeAction.Add, e.Action);
                                                Assert.AreEqual(new[] {0, 0}, e.MultiDimensionalIndex);
                                                Assert.AreEqual(new[] {3, 2}, e.Shape);
                                                    //inserted a 3x2 block
                                                Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, e.Items);
                                            };

            //insert 2 slices on the first dimension
            array.InsertAt(0, 0, 3, new object[] {1.0, 2.0, 3.0, 4.0, 5.0, 6.0});

            Assert.AreEqual(1, callCount);
            Assert.AreEqual("{{1, 2}, {3, 4}, {5, 6}, {0, 0}, {0, 0}}", array.ToString());
        }


        [Test]
        public void InsertAtGivesAggregatedCollectionChangedEvent()
        {
            var array = new MultiDimensionalArray<double>(new[] { 2, 2 });

            var callCount = 0;
            array.CollectionChanged += (s, e) =>
            {
                callCount++;
                var args = (MultiDimensionalArrayChangingEventArgs)e;
                Assert.AreEqual(NotifyCollectionChangeAction.Add, args.Action);
                Assert.AreEqual(new[] { 0, 0 }, args.MultiDimensionalIndex);
                Assert.AreEqual(new[] { 3, 2 }, args.Shape);
                //inserted a 3x2 block
                Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, args.Items);
            };

            //insert 2 slices on the first dimension
            array.InsertAt(0, 0, 3, new object[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 });

            Assert.AreEqual(1, callCount);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Number of values to insert does not match shape of insert. Expected 6 values got 2")]
        public void ThrowExceptionIfInsertedValuesCountIsIncorrect()
        {
            //array of 4x3
            var array = new MultiDimensionalArray<double>(new[] {4, 3});

            //insert 2 slices of dim1 ...expected 2*3 = 6 values insert 2 should give exception
            array.InsertAt(0, 0, 2, new object[] {1.0, 2.0});
        }


        [Test]
        public void AutoSortedArrayAddsAtCorrectIndex()
        {
            var array = new MultiDimensionalArray<double>(new[] {1.0, 2.0, 4.0, 5.0}, new[] {4})
                            {
                                IsAutoSorted = true
                            };
            var collectionChangingCount = 0;
            var collectionChangedCount = 0;

            //in should be inserted at index 2
            //changing shows intention..
            array.CollectionChanging += (s, e) =>
                                            {
                                                Assert.AreEqual(NotifyCollectionChangeAction.Add, e.Action);
                                                Assert.AreEqual(3.0, e.Items[0]);
                                                Assert.AreEqual(4, e.Index);
                                                collectionChangingCount++;
                                            };

            //this is what happened.
            array.CollectionChanged += (s, e) =>
                                           {
                                               Assert.AreEqual(NotifyCollectionChangeAction.Add, e.Action);
                                               Assert.AreEqual(3.0, e.Items[0]);
                                               Assert.AreEqual(2, e.Index);
                                               collectionChangedCount++;
                                           };

            //action! add a 3.0
            array.Add(3.0);

            Assert.AreEqual(1, collectionChangingCount);
            Assert.AreEqual(1, collectionChangedCount);
        }
        
        [Test]
        public void ReplacingValueMaintainsASortOrder()
        {
            var shape = new[] {3};
            var values = new[] {1.0, 3.0, 5.0};

            var array = new MultiDimensionalArray<double>(values, shape) {IsAutoSorted = true};

            array[0] = 4.0;

            Assert.AreEqual(new [] { 3.0, 4.0, 5.0 }, array);
        }

        [Test]
        public void ReplacingValueMaintainsASortOrderAndCheckEvents()
        {
            var shape = new[] {3};
            var values = new[] {1.0, 3.0, 5.0};
            
            var array = new MultiDimensionalArray<double>(values, shape) { IsAutoSorted = true };

            var collectionChangingCount = 0;
            var collectionChangedCount = 0;

            //in should be inserted at index 2
            //changing shows intention..
            array.CollectionChanging += (s, e) =>
                                            {
                                                Assert.AreEqual(NotifyCollectionChangeAction.Replace, e.Action);
                                                Assert.AreEqual(2.0, e.Items[0]);

                                                Assert.AreEqual(2, e.Index);
                                                collectionChangingCount++;
                                            };

            //this is what happened.
            array.CollectionChanged += (s, e) =>
                                           {
                                               Assert.AreEqual(NotifyCollectionChangeAction.Replace, e.Action);
                                               Assert.AreEqual(2.0, e.OldIndex);
                                               Assert.AreEqual(1.0, e.Index);
                                               Assert.AreEqual(2.0, e.Items[0]);

                                               //what about old value...do we need it? or should we have saved it in changing
                                               //Assert.AreEqual(5.0,e.ol);
                                               collectionChangedCount++;
                                           };

            //the classic example...change the 5 to a two and you have a lot of this going on..
            array[2] = 2.0;

            Assert.AreEqual(1, collectionChangingCount);
            Assert.AreEqual(1, collectionChangedCount);
        }


        [Test, NUnit.Framework.Category(TestCategory.Performance)]
        [NUnit.Framework.Category(TestCategory.WorkInProgress)] // slow
        public void PerformanceAddValues()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>();

            const int valuesToAdd = 200000;
            var valuesToAddArray = Enumerable.Range(1, valuesToAdd).Select(Convert.ToDouble).ToArray();
            TestHelper.AssertIsFasterThan(150, () =>
                                              array.AddRange(valuesToAddArray));
            
            
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Adding range of values for sorted array is only possible if these values are all bigger than the current max and sorted")]
        public void AddRangeToSortedArrayThrowsExceptionIfRangeContainsValuesSmallerThenMaxValue()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>(new[] {1.0, 2.0, 3.0}, new[] {3}){IsAutoSorted = true};
            array.AddRange(new[]{0.0,2.0,3.0});
            
        }


        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Adding range of values for sorted array is only possible if these values are all bigger than the current max and sorted")]
        public void AddUnsortedRangeGivesException()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>()
                                                       {IsAutoSorted = true};
            //not sorted..should throw
            array.AddRange(new[] {5.0, 2.0, 3.0});

        }

        [Test]
        public void RemoveDoesNothingWhenValueIsNotInArray()
        {
            IMultiDimensionalArray<double> array = new MultiDimensionalArray<double>()
                                                       {IsAutoSorted = true};
            array.Add(0.0);
            array.Add(10.0);

            //remove something that was never there. shoud have no effect (just like in List<T>
            array.Remove(11.0d);

            Assert.AreEqual(new[]{0.0,10.0},array);
        }

        [Test]
        public void BubblePropertyChangesOfInsertedObject()
        {
            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();

            IMultiDimensionalArray<TestNotifyPropertyChangedObject> array = new MultiDimensionalArray<TestNotifyPropertyChangedObject>
                                                                                {
                                                                                    IsAutoSorted = false
                                                                                };
            array.Add(new TestNotifyPropertyChangedObject());

            array.InsertAt(0, 0, 1, new[] { testNotifyPropertyChangedObject });

            TestHelper.AssertPropertyChangedIsFired(array, 1, testNotifyPropertyChangedObject.FireChanged);
        }

        [Test]
        public void BubblePropertyChangesOfInsertedObjectViaSetValues()
        {
            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();
            var array = new MultiDimensionalArray<TestNotifyPropertyChangedObject> { IsAutoSorted = false };

            array.SetValues(new[] { testNotifyPropertyChangedObject });

            TestHelper.AssertPropertyChangedIsFired(array, 1, testNotifyPropertyChangedObject.FireChanged);
        }

        [Test]
        public void BubblePropertyChangedOfInsertObject2()
        {
            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();

            var array = new MultiDimensionalArray<TestNotifyPropertyChangedObject>
            {
                IsAutoSorted = false
            };
            array.Insert(0, testNotifyPropertyChangedObject);

            TestHelper.AssertPropertyChangedIsFired(array,1,testNotifyPropertyChangedObject.FireChanged);
        }
        [Test]
        public void UnsubscribePropertyChanged()
        {
            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject();

            IMultiDimensionalArray<TestNotifyPropertyChangedObject> array =
                new MultiDimensionalArray<TestNotifyPropertyChangedObject>();
            
            //remove the item from the array
            array.Insert(0, testNotifyPropertyChangedObject);
            TestHelper.AssertPropertyChangedIsFired(array, 1, testNotifyPropertyChangedObject.FireChanged);

            array.Remove(testNotifyPropertyChangedObject);
            TestHelper.AssertPropertyChangedIsFired(array, 0,testNotifyPropertyChangedObject.FireChanged);

            //reinsert & replace
            array.Insert(0,testNotifyPropertyChangedObject);
            array[0] = new TestNotifyPropertyChangedObject {Name = "new"};
            TestHelper.AssertPropertyChangedIsFired(array, 0, testNotifyPropertyChangedObject.FireChanged);

            //removeat 
            array.Insert(0, testNotifyPropertyChangedObject);
            array.RemoveAt(0);
            TestHelper.AssertPropertyChangedIsFired(array, 0, testNotifyPropertyChangedObject.FireChanged);

        }

        [Test]
        public void AddingValueToSortedArrayKeepsOldSubscribtion()
        {
            var array = new MultiDimensionalArray<TestNotifyPropertyChangedObject> { IsAutoSorted = true };

            var testNotifyPropertyChangedObject = new TestNotifyPropertyChangedObject {Value = 10};
            array.Add(testNotifyPropertyChangedObject);
            array.Add(new TestNotifyPropertyChangedObject { Value = 5 });//this will cause sorting

            int callCount = 0;
            ((INotifyPropertyChanged) array).PropertyChanged += (s, e) => { callCount++; };
            testNotifyPropertyChangedObject.FireChanged();

            Assert.AreEqual(1,callCount);
        }


    }
}
