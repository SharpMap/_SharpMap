using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.TestUtils;
using log4net;
using log4net.Config;
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
        [Category("Performance")]
        public void CheckArrayPerormance()
        {
            int[] lengths = new[] {100, 200, 300};

            DateTime startTime = DateTime.Now;

            Array values = Array.CreateInstance(typeof(int), lengths, new[] { 0, 0, 0 });

            for (int i = 0; i < lengths[0]; i++)
            {
                for (int j = 0; j < lengths[1]; j++)
                {
                    for (int k = 0; k < lengths[2]; k++)
                    {
                        values.SetValue(i + j * 1000 + k * 1000000, i, j, k);
                    }
                }
            }

            TimeSpan elapsedTime = DateTime.Now - startTime;

            log.InfoFormat("values set in {0:0.000000} milliseconds.", elapsedTime.TotalMilliseconds);

            Assert.Less(elapsedTime.TotalMilliseconds, 1500);
        }

        [Test]
        public void CheckListPerformence()
        {
            int[] lengths = new[] { 100, 100, 30 };

            DateTime startTime = DateTime.Now;

            ArrayList values = new ArrayList();

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

            TimeSpan elapsedTime = DateTime.Now - startTime;

            log.InfoFormat("loaded in {0:0.000000} milliseconds.", elapsedTime.TotalMilliseconds);
            Assert.Less(elapsedTime.TotalMilliseconds, 50);
        }
    }
}