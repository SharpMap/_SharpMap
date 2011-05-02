using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DelftTools.Functions.Generic;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Reflection;
using log4net;
using log4net.Config;
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
            array.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
                                           {
                                               var args = (MultiDimensionalArrayChangedEventArgs)e;
                                               switch (callCount++)
                                               {
                                                   case 0: Assert.IsTrue(new[] { 1, 0, 0 }.SequenceEqual(args.MultiDimensionalIndex));
                                                       break;
                                                   case 1: Assert.IsTrue(new[] { 1, 0, 1 }.SequenceEqual(args.MultiDimensionalIndex));
                                                       break;
                                                   case 2: Assert.IsTrue(new[] { 1, 1, 0 }.SequenceEqual(args.MultiDimensionalIndex));
                                                       break;
                                                   case 3: Assert.IsTrue(new[] { 1, 1, 1 }.SequenceEqual(args.MultiDimensionalIndex));
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
            array.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
                                           {
                                               var args = (MultiDimensionalArrayChangedEventArgs)e;
                                               Assert.AreEqual(new[] { 1 }, args.MultiDimensionalIndex);
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
            array.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
                                           {
                                               var args = (MultiDimensionalArrayChangedEventArgs)e;
                                               Assert.AreEqual(new[] { 1, 1 }, args.MultiDimensionalIndex);
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
            array.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
                                           {
                                               var args = (MultiDimensionalArrayChangedEventArgs)e;
                                               Assert.AreEqual(new[] { 1 }, args.MultiDimensionalIndex);
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

        [Test, Category("Performance")]
        public void PerformanceChangeSizeOfFirstDimensions_WithEvents()
        {
            DateTime t = DateTime.Now;

            // increasing the 1st dimension should be fast. 
            // For example when we add a new measurement for a 3x3 grid
            IMultiDimensionalArray array = new MultiDimensionalArray(0, 5, 5);
            array.CollectionChanged += delegate { };

            //add more data throught and increase the 1st dimenion-size
            const int valuesToAdd = 100000;
            for (int i = 0; i < valuesToAdd; i++)
            {
                array.Resize(new[] { i + 1, 5, 5 });
                array[i, 1, 1] = 5; //everywhere a five.
            }

            double dt = DateTime.Now.Subtract(t).TotalMilliseconds;
            Assert.Less(dt, 800);

            log.DebugFormat("Added {0} values in {1} ms", valuesToAdd, dt);

            for (int i = 0; i < valuesToAdd - 1; i++)
            {
                Assert.AreEqual(5, array[i, 1, 1]);
            }
        }

        [Test, Category("Performance")]
        public void PerformanceChangeSizeOfFirstDimensions_WithoutEvents()
        {
            DateTime t = DateTime.Now;

            // increasing the 1st dimension should be fast. 
            // For example when we add a new measurement for a 3x3 grid
            IMultiDimensionalArray array = new MultiDimensionalArray<int>(0, 5, 5);

            array.FireEvents = false;
            //add more data throught and increase the 1st dimenion-size
            const int valuesToAdd = 100000;
            for (int i = 0; i < valuesToAdd; i++)
            {
                array.Resize(new[] { i + 1, 5, 5 });
                array[i, 1, 1] = 5; //everywhere a five.
            }

            double dt = DateTime.Now.Subtract(t).TotalMilliseconds;
            Assert.Less(dt, 300);

            log.DebugFormat("Added {0} values in {1} ms", valuesToAdd, dt);

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
            multiDimensionalArray = new MultiDimensionalArray<int>(new List<int> { 1, 2, 3, 4 }, new[] { 2, 2 });
            multiDimensionalArray.CollectionChanged +=
                delegate { callCount++; };
            callCount = 0;

            //generate two change events
            multiDimensionalArray.Resize(3, 2);
            Assert.AreEqual(2, callCount);
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
        public void IncrementMultiDimensionalIndex()
        {
            var shape = new[] { 3, 3, 3 };
            bool result;

            var index = new[] { 0, 0, 0 };
            var dimension = 0;
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(result);
            Assert.IsTrue(index.SequenceEqual(new[] { 1, 0, 0 }));
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 2, 0, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsFalse(result);

            index = new[] { 0, 0, 0 };
            dimension = 1;
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 0, 1, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 0, 2, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 1, 0, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 1, 1, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 1, 2, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 2, 0, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 2, 1, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsTrue(index.SequenceEqual(new[] { 2, 2, 0 }));
            Assert.IsTrue(result);
            result = MultiDimensionalArrayHelper.IncrementIndex(index, shape, dimension);
            Assert.IsFalse(result);
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

            int callCount = 0;
            array.CollectionChanged += delegate
                                           {
                                               callCount++;
                                           };
            
            array.Move(dimension, startIndex, length, newIndex);
            
            Assert.AreEqual(new[] {4, 5, 1, 2, 3}, array);
            //every value changed
            Assert.AreEqual(5,callCount);
            
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
        [Ignore("WorkInProgress")]
        public void Parse3DFromString()
        {
            var array = MultiDimensionalArray<int>.Parse("{ {{0, 1}, {2, 3}}, {{4, 5}, {6, 7}} }"); // 2x2x2

            array.Rank
                .Should("array rank").Be.EqualTo(3);

            array.Shape
                .Should("have shape of 2x2x2").Have.SameSequenceAs(new[] { 2, 2, 2 });

            array.Last()
                .Should("check last element").Be.EqualTo(7);
        }
    }
}
