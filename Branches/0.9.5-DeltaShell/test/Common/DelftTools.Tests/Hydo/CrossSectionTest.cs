using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.Hydro.Helpers;
using DelftTools.TestUtils;
using DelftTools.Utils.Collections;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionTest
    {
        [Test]
        public void MakeDefinitionLocalCreatesAShiftedCopyOfInnerDefinition()
        {
            var innerDefinition = CrossSectionDefinitionYZ.CreateDefault();

            //create a shifted proxy
            const double levelShift = 1.0;
            var proxy = new CrossSectionDefinitionProxy(innerDefinition) {LevelShift = levelShift};

            var hydroNetwork = HydroNetworkHelper.GetSnakeHydroNetwork(1);

            var crossSection = new CrossSection(proxy) { Branch = hydroNetwork.Channels.First() };

            crossSection.MakeDefinitionLocal();

            Assert.IsFalse(crossSection.Definition.IsProxy);
            Assert.IsTrue(crossSection.Definition is CrossSectionDefinitionYZ);
            Assert.AreEqual(crossSection.Definition.Profile,innerDefinition.Profile.Select(c=>new Coordinate(c.X,c.Y+levelShift)).ToList());
        }
        
        [Test]
        public void MakeDefinitionSharedCopiesDefinitionToNetwork()
        {
            var crossSectionDefinitionYZ = CrossSectionDefinitionYZ.CreateDefault();

            var hydroNetwork = HydroNetworkHelper.GetSnakeHydroNetwork(1);

            var crossSection = new CrossSection(crossSectionDefinitionYZ) { Branch = hydroNetwork.Channels.First() };

            Assert.AreEqual(0,hydroNetwork.SharedCrossSectionDefinitions.Count);

            crossSection.ShareDefinitionAndChangeToProxy();

            Assert.AreEqual(1, hydroNetwork.SharedCrossSectionDefinitions.Count);
            Assert.AreEqual(crossSectionDefinitionYZ,hydroNetwork.SharedCrossSectionDefinitions.First());
        }
        [Test]
        public void CopyFrom()
        {
            var type = new CrossSectionSectionType {Name = "Main"};
            var sourceDefinition = new CrossSectionDefinitionYZ();
            sourceDefinition.Sections.Add(new CrossSectionSection { MinY = 0, MaxY = 10, SectionType = type });

            var sourceCrossSection = new CrossSection(sourceDefinition);

            var targetDefinition = new CrossSectionDefinitionYZ();
            var targetCrossSection = new CrossSection(targetDefinition);

            targetCrossSection.CopyFrom(sourceCrossSection);
            
            Assert.AreEqual("Main",targetCrossSection.Definition.Sections[0].SectionType.Name);
        }

        [Test]
        public void Clone()
        {
            var crossSection = new TestCrossSectionDefinition("Test",0)
            {
                Thalweg = 3.0,
            };
            var type = new CrossSectionSectionType();
            crossSection.Sections.Add(new CrossSectionSection { MinY = 0, MaxY = 10, SectionType = type });

            var clone = (TestCrossSectionDefinition)crossSection.Clone();
            
            Assert.AreEqual(crossSection.Thalweg,clone.Thalweg);
            Assert.AreEqual(crossSection.Sections.Count, clone.Sections.Count);
            Assert.AreNotSame(crossSection.Sections[0], clone.Sections[0]);
            Assert.AreEqual(crossSection.Sections[0].MinY, clone.Sections[0].MinY);
            Assert.AreEqual(crossSection.Sections[0].MaxY, clone.Sections[0].MaxY);
            Assert.AreSame(crossSection.Sections[0].SectionType, clone.Sections[0].SectionType);
        }

        [Test]
        public void ChangeInStorageDoesNotChangeProfile()
        {
            var crossSection = new CrossSectionDefinitionXYZ();

            var coordinates = new[]
                         {
                             new Coordinate(0, 0),
                             new Coordinate(2, 0),
                             new Coordinate(4, -10),
                             new Coordinate(6, -10),
                             new Coordinate(8, 0),
                             new Coordinate(10, 0)
                         };
            
            //make geometry on the y/z plane
            crossSection.Geometry = new LineString(coordinates.Select(c=>new Coordinate(0,c.X,c.Y)).ToArray());


            //since the profile is defined on the y/z plane we can ignore the x values
            Assert.AreEqual(coordinates, crossSection.Profile);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException),ExpectedMessage = "XYZ definitions can not be shared")]
        public void CrossSectionWithXYZDefinitionCanNotShareDefiniton()
        {
            var crossSectionDefinitionXYZ = new CrossSectionDefinitionXYZ();
            var crossSection = new CrossSection(crossSectionDefinitionXYZ);
            crossSection.ShareDefinitionAndChangeToProxy();
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Performance)]
        public void ChangeInDefinitionUpdatesGeometry()
        {
            //v-shaped cs 100 wide
            var crossSectionDefinitionYZ = new CrossSectionDefinitionYZ("");
            crossSectionDefinitionYZ.YZDataTable.AddCrossSectionYZRow(0, 100, 0);
            crossSectionDefinitionYZ.YZDataTable.AddCrossSectionYZRow(50, 0, 0);
            crossSectionDefinitionYZ.YZDataTable.AddCrossSectionYZRow(100, 100, 0);
            crossSectionDefinitionYZ.Thalweg = 50;

            //horizontal line
            var network = HydroNetworkHelper.GetSnakeHydroNetwork(new Point(0,0),new Point(100,0));
            var branch = network.Channels.First();
            ICrossSection crossSection = HydroNetworkHelper.AddCrossSectionDefinitionToBranch(branch, crossSectionDefinitionYZ, 50);
            

            var expectedGeometry = new LineString(new[] {new Coordinate(50, 50), new Coordinate(50, -50)});
            //use equals exact because rounding errors occur
            Assert.IsTrue(expectedGeometry.EqualsExact(crossSection.Geometry, 0.0001));
            
            //action : change the profile
            crossSectionDefinitionYZ.YZDataTable[0].Yq = -20;
            
            expectedGeometry = new LineString(new[] { new Coordinate(50, 70), new Coordinate(50, -50) });
            Assert.IsTrue(expectedGeometry.EqualsExact(crossSection.Geometry, 0.0001));

            //action: change the thalweg
            crossSectionDefinitionYZ.Thalweg = 40;

            expectedGeometry = new LineString(new[] { new Coordinate(50, 60), new Coordinate(50, -60) });
            Assert.IsTrue(expectedGeometry.EqualsExact(crossSection.Geometry, 0.0001));
        }

        [Test]
        [NUnit.Framework.Category(TestCategory.Performance)]
        public void GetGeometryWidthYZDefinitionShouldBeFast()
        {
            var yz = new CrossSectionDefinitionYZ("");
            yz.YZDataTable.AddCrossSectionYZRow(10, 100, 0);
            yz.YZDataTable.AddCrossSectionYZRow(9, 90, 0);
            yz.YZDataTable.AddCrossSectionYZRow(8, 80, 0);
            yz.YZDataTable.AddCrossSectionYZRow(7, 60, 0);
            yz.YZDataTable.AddCrossSectionYZRow(6, 70, 0);
            yz.YZDataTable.AddCrossSectionYZRow(5, 40, 0);
            yz.YZDataTable.AddCrossSectionYZRow(4, 40, 0);
            yz.YZDataTable.AddCrossSectionYZRow(3, 20, 0);

            var network = HydroNetworkHelper.GetSnakeHydroNetwork(1);
            var branch = network.Channels.First();
            var crossSection = HydroNetworkHelper.AddCrossSectionDefinitionToBranch(branch, yz, 30);
            TestHelper.AssertIsFasterThan(50, () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    var geo = crossSection.Geometry;
                }
            });
        }

    }
}