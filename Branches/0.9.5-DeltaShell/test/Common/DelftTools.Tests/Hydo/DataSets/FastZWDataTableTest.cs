using System;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.DataSets
{
    [TestFixture]
    public class FastZWDataTableTest
    {
        readonly Random random = new Random();
        
        [Test]
        [Category(TestCategory.Performance)]
        public void SerializeAndDeserialize()
        {

            FastDataTableTestHelper.TestSerializationIsFastAndCorrect<FastZWDataTable>(25, 30, (t) =>
                                                                                               t.AddCrossSectionZWRow(
                                                                                                   random.NextDouble(),
                                                                                                   random.NextDouble(),
                                                                                                   random.NextDouble()));
        }

    }
}