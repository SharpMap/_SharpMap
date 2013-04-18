using System;
using System.Data;
using System.Linq;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.Hydro.Helpers;
using DelftTools.TestUtils;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionDefinitionZWTest
    {
        [Test]
        public void EmptyProfiles()
        {
            var crossSection = new CrossSectionDefinitionZW();

            var profile = crossSection.Profile;
            var flowProfile = crossSection.FlowProfile;

            Assert.AreEqual(0, profile.Count());
            Assert.AreEqual(0, flowProfile.Count());
        }

        [Test]
        public void LevelShiftDoesNotCauseUniqueException()
        {
            var crossSection = new CrossSectionDefinitionZW();
            //simple V profile
            crossSection.ZWDataTable.AddCrossSectionZWRow(10, 100, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(6, 50, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(0, 0, 0);

            //level shift it by -4...this makes two rows 6 causing a unique constraint exception
         
            crossSection.ShiftLevel(-4);
        }

        [Test]
        [ExpectedException(typeof(ConstraintException), ExpectedMessage = "Column 'Z' is constrained to be unique.  Value '6' is already present.")]
        public void ZConstraintWorksAfterLevelShift()
        {
            var crossSection = new CrossSectionDefinitionZW();
            //simple V profile
            crossSection.ZWDataTable.AddCrossSectionZWRow(10, 100, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(6, 50, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(0, 0, 0);

            //level shift it by 0
            crossSection.ShiftLevel(0);

            //change the first row to 6..this should cause a constraint exception
            crossSection.ZWDataTable[0].Z = 6;

        }



        [Test]
        public void TestProfileMatchesDataTable()
        {
            var crossSection = new CrossSectionDefinitionZW();
            //simple V profile
            crossSection.ZWDataTable.AddCrossSectionZWRow(10, 100, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(6, 50, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(0, 0, 0);

            var profileY = new double[] { -50, -25, 0, 25, 50 };
            var profileZ = new double[] {10, 6, 0, 6, 10};

            var flowProfileY = new double[] { -30, -5, 0, 5, 30 };
            var flowProfileZ = new double[] { 10, 6, 0, 6, 10 };

            Assert.AreEqual(profileY, crossSection.Profile.Select(c => c.X).ToArray());
            Assert.AreEqual(profileZ, crossSection.Profile.Select(c => c.Y).ToArray());
            Assert.AreEqual(flowProfileY, crossSection.FlowProfile.Select(c => c.X).ToArray());
            Assert.AreEqual(flowProfileZ, crossSection.FlowProfile.Select(c => c.Y).ToArray());
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Storage Width cannot exceed Total Width.")]
        public void StorageWidthMustBeLessThanNormalWidthAdd()
        {
            var crossSection = new CrossSectionDefinitionZW();

            crossSection.ZWDataTable.AddCrossSectionZWRow(20, 200, 400); //exception
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Storage Width cannot exceed Total Width.")]
        public void StorageWidthMustBeLessThanNormalWidthEdit()
        {
            var crossSection = new CrossSectionDefinitionZW();

            //simple \_/ profile
            crossSection.ZWDataTable.AddCrossSectionZWRow(20, 200, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(15, 150, 40);
            crossSection.ZWDataTable.AddCrossSectionZWRow(10, 100, 40);

            crossSection.ZWDataTable[1].StorageWidth = 300.0; //exception
        }


        
        [Test]
        public void SetReferenceLevelHeightWidthWidthTest()
        {
            var crossSection = new CrossSectionDefinitionZW();
            crossSection.SetWithHfswData(new[]
                                             {
                                                 new HeightFlowStorageWidth(0, 10.0, 10.0),
                                                 new HeightFlowStorageWidth(10, 100.0, 100.0)
                                             });


            Assert.AreEqual(0.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(10.0, crossSection.HighestPoint, 1.0e-6);

            crossSection.ShiftLevel(111);

            Assert.AreEqual(111.0, crossSection.ZWDataTable[0].Z, 1.0e-6);
            Assert.AreEqual(121.0, crossSection.ZWDataTable[1].Z, 1.0e-6);

            Assert.AreEqual(111.0, crossSection.LowestPoint, 1.0e-6);
            Assert.AreEqual(121.0, crossSection.HighestPoint, 1.0e-6);
        }

        [Test]
        public void Clone()
        {
            var crossSection = new CrossSectionDefinitionZW
            {
                SummerDike = new SummerDike()
                {
                    CrestLevel = 1,
                    FloodSurface = 2,
                    TotalSurface = 3,
                    FloodPlainLevel = 4
                }
            };
            crossSection.ZWDataTable.AddCrossSectionZWRow(4, 5, 2);

            var clone = (CrossSectionDefinitionZW)crossSection.Clone();

            ReflectionTestHelper.AssertPublicPropertiesAreEqual(crossSection.SummerDike, clone.SummerDike);
            Assert.AreNotSame(crossSection.SummerDike, clone.SummerDike);

            Assert.IsTrue(crossSection.ZWDataTable.ContentEquals(clone.ZWDataTable));
        }

        [Test]
        public void CopyFrom()
        {
            var crossSection = new CrossSectionDefinitionZW
            {
                Thalweg = 5.0,
                SummerDike = new SummerDike
                                {
                                    CrestLevel = 1,
                                    FloodSurface = 2,
                                    TotalSurface = 3,
                                    FloodPlainLevel = 4
                                }
            };
            crossSection.ZWDataTable.AddCrossSectionZWRow(4, 5, 2);

            var copyFrom = new CrossSectionDefinitionZW
            {
                Thalweg = 1.0,
                SummerDike = new SummerDike
                {
                    CrestLevel = 4,
                    FloodSurface = 3,
                    TotalSurface = 2,
                    FloodPlainLevel = 1
                }
            };

            copyFrom.CopyFrom(crossSection);

            Assert.AreEqual(crossSection.Thalweg, copyFrom.Thalweg);
            ReflectionTestHelper.AssertPublicPropertiesAreEqual(crossSection.SummerDike, copyFrom.SummerDike);
            Assert.AreNotSame(crossSection.SummerDike, copyFrom.SummerDike);

            Assert.IsTrue(crossSection.ZWDataTable.ContentEquals(copyFrom.ZWDataTable));
        }

        [Test]
        public void RemoveInvalidSections()
        {
            var mainType = new CrossSectionSectionType
                               {
                                   Name = "Main"
                               };

            var crossSection = new CrossSectionDefinitionZW();
            crossSection.Sections.Add(new CrossSectionSection {SectionType = mainType});

            Assert.AreEqual(1,crossSection.Sections.Count);

            //now rename the type and call 
            mainType.Name = "newName";
            crossSection.RemoveInvalidSections();

            Assert.AreEqual(0,crossSection.Sections.Count);
        }
    }
}