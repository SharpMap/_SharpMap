using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.Hydro.Helpers;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionDefinitionXYZTest
    {
        [Test]
        public void EmptyProfiles()
        {
            var crossSection = new CrossSectionDefinitionXYZ();
            
            crossSection.Geometry = new LineString(new ICoordinate[] {});

            var profile = crossSection.Profile;
            var flowProfile = crossSection.FlowProfile;

            Assert.AreEqual(0, profile.Count());
            Assert.AreEqual(0, flowProfile.Count());
        }

        [Test]
        public void SetGeometry()
        {
            var crossSection = new CrossSectionDefinitionXYZ();

            crossSection.Geometry = 
                new LineString(new ICoordinate[]
                                   {
                                       new Coordinate(0, 0, 0), new Coordinate(2, 2, -2), new Coordinate(4, 2, -2),
                                       new Coordinate(6, 0, 0)
                                   }); //xyz

            var diag = 2.0*Math.Sqrt(2);

            var expectedProfileY = new double[] { 0, diag, diag+2, 2*diag+2};
            var expectedProfileZ = new double[] { 0, -2, -2, 0};

            var profileY = crossSection.Profile.Select(p => p.X).ToList();
            var profileZ = crossSection.Profile.Select(p => p.Y).ToList();

            for (int i = 0; i < expectedProfileY.Length; i++)
            {
                Assert.AreEqual(expectedProfileY[i], profileY[i], 0.001); //2d profile
                Assert.AreEqual(expectedProfileZ[i], profileZ[i], 0.001);
            }
        }

        [Test]
        public void SetDifferentGeometries()
        {
            var crossSection = new CrossSectionDefinitionXYZ();

            var coordinates = new List<ICoordinate>
                                  {
                                      new Coordinate(0, 0, 0),
                                      new Coordinate(2, 2, -2),
                                      new Coordinate(4, 2, -2),
                                      new Coordinate(6, 0, 0)
                                  };

            var geometry1 = new LineString(coordinates.ToArray());
            coordinates.Insert(2, new Coordinate(3, 2, -2));
            var geometry2 = new LineString(coordinates.Select(c=>c.Clone() as ICoordinate).ToArray()); //full clone
            coordinates.RemoveAt(1);
            var geometry3 = new LineString(coordinates.Select(c => c.Clone() as ICoordinate).ToArray());
            coordinates = coordinates.Select(c => new Coordinate(c.X+10, c.Y, c.Z)).OfType<ICoordinate>().ToList();
            var geometry4 = new LineString(coordinates.Select(c => c.Clone() as ICoordinate).ToArray());

            crossSection.Geometry = geometry1;
            int count = 4; //define some storage
            for(int i = 0; i < count; i++)
            {
                crossSection.XYZDataTable[i].DeltaZStorage = i; 
            }

            geometry1[2].X = 5; //move a point
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(crossSection.XYZDataTable[i].DeltaZStorage, i); //expect no changes
            }

            int j = 0;
            crossSection.Geometry = geometry2; //add a point
            for (int i = 0; i < count+1; i++)
            {
                if (i != 2)
                {
                    Assert.AreEqual(crossSection.XYZDataTable[i].DeltaZStorage, j);
                    j++;
                }
            }

            crossSection.Geometry = geometry3; //remove a point
            double[] expected = {0.0, 0.0, 2.0, 3.0};
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(crossSection.XYZDataTable[i].DeltaZStorage, expected[i]);
            }

            crossSection.Geometry = geometry4; //move all points
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(crossSection.XYZDataTable[i].DeltaZStorage, expected[i]);
            }
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Cannot add / delete rows from XYZ Cross Section")]
        public void AddToDataTableFails()
        {
            var crossSection = new CrossSectionDefinitionXYZ();

            crossSection.Geometry =
                new LineString(new ICoordinate[]
                                   {
                                       new Coordinate(0, 0, 0), new Coordinate(2, 2, -2), new Coordinate(4, 2, -2),
                                       new Coordinate(6, 0, 0)
                                   });

            crossSection.XYZDataTable.AddCrossSectionXYZRow(3, 0, 0);
        }

        [Test]
        public void TableMatchesGeometry()
        {
            var crossSection = new CrossSectionDefinitionXYZ();

            crossSection.Geometry =
                new LineString(new ICoordinate[]
                                   {
                                       new Coordinate(0, 0, 0), new Coordinate(2, 2, -2), new Coordinate(4, 2, -2),
                                       new Coordinate(6, 0, 0)
                                   });

            Assert.AreEqual(crossSection.Geometry.Coordinates.Length, crossSection.XYZDataTable.Rows.Count);

            var diag = 2.0 * Math.Sqrt(2);

            var expectedProfileY = new double[] { 0, diag, diag + 2, 2 * diag + 2 };
            var expectedProfileZ = new double[] { 0, -2, -2, 0 };
            var expectedStorageZ = new double[] { 0, 0, 0, 0 };

            var rows = crossSection.XYZDataTable.Rows;

            for (int i = 0; i < expectedProfileY.Length; i++)
            {
                var xyzRow = rows[i] as CrossSectionDataSet.CrossSectionXYZRow;

                Assert.AreEqual(expectedProfileY[i], xyzRow.Yq, 0.001); //2d profile
                Assert.AreEqual(expectedProfileZ[i], xyzRow.Z, 0.001);
                Assert.AreEqual(expectedStorageZ[i], xyzRow.DeltaZStorage, 0.001);
            }
        }

        [Test]
        public void ChangingGeometryChangesTable()
        {
            var crossSection = new CrossSectionDefinitionXYZ();

            crossSection.Geometry =
                new LineString(new ICoordinate[]
                                   {
                                       new Coordinate(0, 0, 0), new Coordinate(2, 2, -2), new Coordinate(4, 2, -2),
                                       new Coordinate(6, 0, 0)
                                   });

            crossSection.Geometry.Coordinates[1].Z = -1;
            crossSection.Geometry = crossSection.Geometry;

            Assert.AreEqual(-1, crossSection.XYZDataTable[1].Z);
        }

        [Test]
        public void SetReferenceLevelGeometry()
        {
            var crossSection = new CrossSectionDefinitionXYZ();
            var coordinates = new List<ICoordinate>
                                  {
                                      new Coordinate(0, 0, 0),
                                      new Coordinate(10, 0, 0),
                                      new Coordinate(30, 0, 0),
                                      new Coordinate(40, 0, 0)
                                  };
            crossSection.Geometry = new LineString(coordinates.ToArray());
            
            Assert.AreEqual(0.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(0.0, crossSection.HighestPoint, 1.0e-6);

            crossSection.ShiftLevel(111);

            Assert.AreEqual(111.0, crossSection.Geometry.Coordinates[0].Z, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.Geometry.Coordinates[0].Z, 1.0e-6);

            Assert.AreEqual(111.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(111.0, crossSection.HighestPoint, 1.0e-6);
        }

        [Test]
        public void Clone()
        {
            var crossSection = new CrossSectionDefinitionXYZ
                                   {
                                       Geometry = new LineString(new ICoordinate[]
                                                                     {
                                                                         new Coordinate(0, 0, 0),
                                                                         new Coordinate(2, 2, -2),
                                                                         new Coordinate(4, 2, -2),
                                                                         new Coordinate(6, 0, 0)
                                                                     })
                                   };
            crossSection.XYZDataTable[0].DeltaZStorage = 2;
            
            var clone = (CrossSectionDefinitionXYZ) crossSection.Clone();
            
            Assert.AreEqual(crossSection.Geometry, clone.Geometry);
            Assert.AreEqual(typeof(FastXYZDataTable), clone.XYZDataTable.GetType());
            Assert.AreEqual(2,clone.XYZDataTable[0].DeltaZStorage);

        }

        
        [Test]
        public void CopyFrom()
        {
            var source = new CrossSectionDefinitionXYZ
                                   {
                                       Geometry = new LineString(new ICoordinate[]
                                                                     {
                                                                         new Coordinate(0, 0, 0),
                                                                         new Coordinate(2, 0, -2),
                                                                         new Coordinate(4, 0, 0),
                                                                     })
                                   };
            const int deltaZStorage = 1;
            source.XYZDataTable[1].DeltaZStorage = deltaZStorage;

            var target = new CrossSectionDefinitionXYZ();

            //action! 
            target.CopyFrom(source);

            //for now we just expect the same geometry
            Assert.AreEqual(source.Geometry, target.Geometry);
            Assert.AreEqual(deltaZStorage, target.XYZDataTable[1].DeltaZStorage);
            Assert.AreEqual(typeof(FastXYZDataTable), target.XYZDataTable.GetType());
        }
    }
}
