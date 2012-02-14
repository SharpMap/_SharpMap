using System.Collections.Generic;
using System.Data;
using System.Linq;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.TestUtils;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionDefinitionYZTest
    {
        [Test]
        public void EmptyProfiles()
        {
            var crossSection = new CrossSectionDefinitionYZ();

            var profile = crossSection.Profile;
            var flowProfile = crossSection.FlowProfile;

            Assert.AreEqual(0, profile.Count());
            Assert.AreEqual(0, flowProfile.Count());
        }
        
        [Test]
        [ExpectedException(typeof(ConstraintException))]
        public void AddNonUniqueTable()
        {
            var crossSection = new CrossSectionDefinitionYZ();
            crossSection.BeginUpdate();
            crossSection.YZDataTable.AddCrossSectionYZRow(0.0, 1.0, 1.0);
            crossSection.YZDataTable.AddCrossSectionYZRow(1.0, 1.0, 1.0);
            crossSection.YZDataTable.AddCrossSectionYZRow(1.0, 1.0, 1.0);
            crossSection.EndUpdate();
        }

        [Test]
        public void GetGeometryWithNoYZReturnsPoint()
        {
            //this is not a requirement but it seems handy. null might also be reasonable
            var crossSection = new CrossSectionDefinitionYZ();
            Branch branch = new Branch
                                {
                                    Geometry = new LineString(new []{new Coordinate(0,0),new Coordinate(100,0)})
                                };

            var geometry = crossSection.GetGeometry(branch, 20.0d);
            Assert.AreEqual(new Point(20,0),geometry);
        }

        
        [Test]
        public void SetReferenceLevelYZTest()
        {
            var crossSection = new CrossSectionDefinitionYZ();
            IEnumerable<ICoordinate> coordinates = new[]
                                          {
                                              new Coordinate(0, 0),
                                              new Coordinate(0.01, -10),
                                              new Coordinate(19.99, -10),
                                              new Coordinate(20, 0)
                                          };
            crossSection.YZDataTable.SetWithCoordinates(coordinates);


            Assert.AreEqual(-10.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(0.0, crossSection.HighestPoint, 1.0e-6);

            crossSection.ShiftLevel(111);

            var yValues = crossSection.Profile.Select(p => p.Y).ToList();
            Assert.AreEqual(111.0, yValues[0], 1.0e-6);
            Assert.AreEqual(101.0, yValues[1], 1.0e-6);
            Assert.AreEqual(101.0, yValues[2], 1.0e-6);
            Assert.AreEqual(111.0, yValues[3], 1.0e-6);

            Assert.AreEqual(101.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.HighestPoint, 1.0e-6);
        }

        [Test]
        public void TestProfileMatchesDataTable()
        {
            var crossSection = new CrossSectionDefinitionYZ();
            //simple V profile
            crossSection.YZDataTable.AddCrossSectionYZRow(0, 10, 2);
            crossSection.YZDataTable.AddCrossSectionYZRow(5, 0, 1);
            crossSection.YZDataTable.AddCrossSectionYZRow(10, 10, 2);

            var profileY = new double[] { 0, 5, 10 };
            var profileZ = new double[] { 10, 0, 10 };

            var flowProfileY = new double[] { 0, 5, 10 };
            var flowProfileZ = new double[] { 12, 1, 12 };

            Assert.AreEqual(profileY, crossSection.Profile.Select(c => c.X).ToArray());
            Assert.AreEqual(profileZ, crossSection.Profile.Select(c => c.Y).ToArray());
            Assert.AreEqual(flowProfileY, crossSection.FlowProfile.Select(c => c.X).ToArray());
            Assert.AreEqual(flowProfileZ, crossSection.FlowProfile.Select(c => c.Y).ToArray());
        }

        [Test]
        public void Clone()
        {
            var crossSectionYZ = new CrossSectionDefinitionYZ();
            
            //simple V profile
            crossSectionYZ.YZDataTable.AddCrossSectionYZRow(0, 10, 2);
            crossSectionYZ.YZDataTable.AddCrossSectionYZRow(5, 0, 1);
            crossSectionYZ.YZDataTable.AddCrossSectionYZRow(10, 10, 2);

            crossSectionYZ.Thalweg = 5.0;

            var clone = (CrossSectionDefinitionYZ)crossSectionYZ.Clone();

            Assert.AreEqual(crossSectionYZ.Thalweg,clone.Thalweg);

            CrossSectionDataSet.CrossSectionYZDataTable yzDataTable = crossSectionYZ.YZDataTable;
            CrossSectionDataSet.CrossSectionYZDataTable cloneYZTable = clone.YZDataTable;
            Assert.AreEqual(yzDataTable.Rows.Count, cloneYZTable.Rows.Count);
            Assert.AreEqual(5.0, clone.Thalweg);
            for (int i = 0;i<yzDataTable.Count;i++)
            {
                Assert.AreEqual(yzDataTable[i].DeltaZStorage, cloneYZTable[i].DeltaZStorage);
                Assert.AreEqual(yzDataTable[i].Yq, cloneYZTable[i].Yq);
                Assert.AreEqual(yzDataTable[i].Z, cloneYZTable[i].Z);
                Assert.AreNotSame(yzDataTable[i], cloneYZTable[i]);
            }

            //assert a change in the original does not affect the clone
            yzDataTable[0].DeltaZStorage = 6;
            Assert.IsTrue(cloneYZTable.GetType() == typeof(FastYZDataTable));
            Assert.AreNotEqual(6,cloneYZTable[0].DeltaZStorage);
        }

        [Test]
        public void CopyFrom()
        {
            var crossSectionYZ = new CrossSectionDefinitionYZ();

            //simple V profile
            crossSectionYZ.YZDataTable.AddCrossSectionYZRow(0, 10, 2);
            crossSectionYZ.YZDataTable.AddCrossSectionYZRow(5, 0, 1);
            crossSectionYZ.YZDataTable.AddCrossSectionYZRow(10, 10, 2);

            crossSectionYZ.Thalweg = 5.0;

            var copyFrom = new CrossSectionDefinitionYZ
            {
                Thalweg = 1.0
            };

            copyFrom.CopyFrom(crossSectionYZ);

            Assert.AreEqual(crossSectionYZ.Thalweg, copyFrom.Thalweg);

            CrossSectionDataSet.CrossSectionYZDataTable yzDataTable = crossSectionYZ.YZDataTable;
            CrossSectionDataSet.CrossSectionYZDataTable copyYZTable = copyFrom.YZDataTable;
            Assert.AreEqual(yzDataTable.Rows.Count, copyYZTable.Rows.Count);
            Assert.AreEqual(5.0, crossSectionYZ.Thalweg);
            for (int i = 0; i < yzDataTable.Count; i++)
            {
                Assert.AreEqual(yzDataTable[i].DeltaZStorage, copyYZTable[i].DeltaZStorage);
                Assert.AreEqual(yzDataTable[i].Yq, copyYZTable[i].Yq);
                Assert.AreEqual(yzDataTable[i].Z, copyYZTable[i].Z);
                Assert.AreNotSame(yzDataTable[i], copyYZTable[i]);
            }

            //assert a change in the original does not affect the clone
            yzDataTable[0].DeltaZStorage = 6;
            Assert.AreNotEqual(6, copyYZTable[0].DeltaZStorage);
        }
    }
}