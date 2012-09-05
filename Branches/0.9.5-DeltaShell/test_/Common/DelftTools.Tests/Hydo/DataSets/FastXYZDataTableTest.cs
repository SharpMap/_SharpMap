using System;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.TestUtils;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.DataSets
{
    [TestFixture]
    public class FastXYZDataTableTest
    {
        readonly Random random = new Random();

        [Test]
        [Category(TestCategory.Performance)]
        public void SerializeAndDeserialize()
        {
            FastDataTableTestHelper.TestSerializationIsFastAndCorrect<FastXYZDataTable>(40, 30,
                                                                                        (t) =>
                                                                                        t.AddCrossSectionXYZRow(
                                                                                            random.NextDouble(),
                                                                                            random.NextDouble(),
                                                                                            random.NextDouble()));
        }
    }
}