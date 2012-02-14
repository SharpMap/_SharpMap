using DelftTools.Hydro.CrossSections.DataSets;
using DelftTools.Hydro.Helpers;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo.Helpers
{
    [TestFixture]
    public class StandardCrossSectionsFactoryTest
    {
        [Test]
        public void GetTabulatedCrossSectionFromTrapezium()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromTrapezium(2, 20, 100);
            var crossSectionZWDataTable = definitionZW.ZWDataTable;

            Assert.AreEqual(3,crossSectionZWDataTable.Count);

            CrossSectionDataSet.CrossSectionZWRow bottomRow = crossSectionZWDataTable[0];
            Assert.AreEqual(0, bottomRow.Z);
            Assert.AreEqual(20, bottomRow.Width);
            Assert.AreEqual(0, bottomRow.StorageWidth);
            
            CrossSectionDataSet.CrossSectionZWRow topRow = crossSectionZWDataTable[1];
            Assert.AreEqual(20, topRow.Z);
            Assert.AreEqual(100, topRow.Width);
            Assert.AreEqual(0, topRow.StorageWidth);

            //there is one more row on top of the top row which closes the trapezium
            CrossSectionDataSet.CrossSectionZWRow closingRow= crossSectionZWDataTable[2];
            Assert.AreEqual(20.000001, closingRow.Z);
            Assert.AreEqual(0, closingRow.Width);
            Assert.AreEqual(0, closingRow.StorageWidth);
        }

        [Test]
        public void GetTabulatedCrossSectionFromEllipse()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromEllipse(100,80);
            //check it is 'closed' om the top
            Assert.AreEqual(0.0, definitionZW.ZWDataTable[definitionZW.RawData.Rows.Count - 1].Width);
        }

        [Test]
        public void GetTabulatedCrossSectionFromArch()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromArch(100, 80,20);
            //check it is 'closed' om the top
            Assert.AreEqual(0.0, definitionZW.ZWDataTable[definitionZW.RawData.Rows.Count - 1].Width);
        }

        [Test]
        public void GetTabulatedCrossSectionFromRectangle()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromRectangle(100, 80, true);
            //check it is 'closed' om the top
            Assert.AreEqual(0.0, definitionZW.ZWDataTable[definitionZW.RawData.Rows.Count - 1].Width);
        }

        [Test]
        public void GetTabulatedCrossSectionFromCunette()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromCunette(100, 80);
            //check it is 'closed' om the top
            Assert.AreEqual(0.0, definitionZW.ZWDataTable[definitionZW.RawData.Rows.Count - 1].Width);
        }

        [Test]
        public void GetTabulatedCrossSectionFromSteelCunette()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromSteelCunette(0.78,0.50,0.80, 0.2,0,28,0);
            //check it is 'closed' om the top
            Assert.AreEqual(0.0, definitionZW.ZWDataTable[definitionZW.RawData.Rows.Count - 1].Width);
        }

        [Test]
        public void GetTabulatedCrossSectionFromEgg()
        {
            var definitionZW = StandardCrossSectionsFactory.GetTabulatedCrossSectionFromEgg(50);
            //check it is 'closed' om the top
            Assert.AreEqual(0.0, definitionZW.ZWDataTable[definitionZW.RawData.Rows.Count - 1].Width);
        }
    }
}