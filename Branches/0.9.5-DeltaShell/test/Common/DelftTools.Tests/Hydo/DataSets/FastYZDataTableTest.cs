using System;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.DataSets
{
    [TestFixture]
    public class FastYZDataTableTest
    {
        readonly Random random = new Random();
        [Test]
        [Ignore("Just a reference. Shows how slow default serialization is (~300ms)")]
        public void SerializationOfNormalTable()
        {
            FastDataTableTestHelper.TestSerializationIsFastAndCorrect<CrossSectionDataSet.CrossSectionYZDataTable>(20,30,
                                                                                       (t) =>
                                                                                           t.AddCrossSectionYZRow(
                                                                                           random.NextDouble(),
                                                                                           random.NextDouble(),
                                                                                           random.NextDouble()));
        }

        
        [Test]
        [Category(TestCategory.Performance)]
        public void SerializeAndDeserialize()
        {
            FastDataTableTestHelper.TestSerializationIsFastAndCorrect<FastYZDataTable>(25, 30,
                                                                                          (t) =>
                                                                                              t.AddCrossSectionYZRow(
                                                                                              random.NextDouble(),
                                                                                              random.NextDouble(),
                                                                                              random.NextDouble()));
        }
    }
}