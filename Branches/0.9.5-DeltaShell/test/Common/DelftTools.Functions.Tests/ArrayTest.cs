using System;
using System.Collections;
using System.Collections.Generic;
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
        [Ignore("we don't test .NET framework, run locally")]
        [Category(TestCategory.Performance)]
        public void Check3DArrayPerormance()
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
                            values.SetValue(0, i, j, k);
                        }
                    }
                }
            };

            TestHelper.AssertIsFasterThan(1500, action);
        }

        [Test]
        [Ignore("we don't test .NET framework, run locally")]
        [Category(TestCategory.Performance)]
        public void Check3DListPerformance()
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
                            values.Add(0);
                        }
                    }
                }
            };

            TestHelper.AssertIsFasterThan(450, action);
        }

        [Test]
        [Ignore("we don't test .NET framework, run locally")]
        [Category(TestCategory.Performance)]
        public void Check1DArrayPerormance()
        {
            int[] lengths = new[] { 1000000 };

            int[] values = new int[lengths[0]];

            Action action = delegate
            {
                for (int i = 0; i < lengths[0]; i++)
                {
                     values.SetValue(0, i);
                }
            };

            TestHelper.AssertIsFasterThan(1500, action);
        }

        [Test]
        [Ignore("we don't test .NET framework, run locally")]
        [Category(TestCategory.Performance)]
        public void Check1DListPerormance()
        {
            int[] lengths = new[] { 1000000 };

            var values = new List<int>(lengths[0]);

            Action action = delegate
            {
                for (int i = 0; i < lengths[0]; i++)
                {
                    values.Add(0);
                }
            };

            TestHelper.AssertIsFasterThan(1500, action);
        }

    }
}