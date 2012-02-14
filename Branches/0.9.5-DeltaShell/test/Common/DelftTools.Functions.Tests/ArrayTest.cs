using System;
using System.Collections;
using DelftTools.TestUtils;
using log4net;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class ArrayTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ArrayTest));

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
        [Category(TestCategory.Performance)]
        public void CheckArrayPerormance()
        {
            int[] lengths = new[] {100, 200, 300};

            Array values = Array.CreateInstance(typeof (int), lengths, new[] {0, 0, 0});

            Action action = delegate
            {
                for (int i = 0; i < lengths[0]; i++)
                {
                    for (int j = 0; j < lengths[1]; j++)
                    {
                        for (int k = 0; k < lengths[2]; k++)
                        {
                            values.SetValue(i + j*1000 + k*1000000, i, j, k);
                        }
                    }
                }
            };

            TestHelper.AssertIsFasterThan(1500, action);
        }

        [Test]
        [Category(TestCategory.Performance)]
        public void CheckListPerformance()
        {
            int[] lengths = new[] { 100, 100, 200 };

            ArrayList values = new ArrayList();

            Action action = delegate
            {
                for (int i = 0; i < lengths[0]; i++)
                {
                    for (int j = 0; j < lengths[1]; j++)
                    {
                        for (int k = 0; k < lengths[2]; k++)
                        {
                            values.Add(i + j + k);
                        }
                    }
                }
            };

            TestHelper.AssertIsFasterThan(450, action);
        }
    }
}