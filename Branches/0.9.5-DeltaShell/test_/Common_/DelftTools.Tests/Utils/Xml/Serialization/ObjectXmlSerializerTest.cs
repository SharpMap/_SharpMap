using DelftTools.Tests.Utils.Xml.Serialization.TestClasses;
using DelftTools.Utils.TestClasses;
using DelftTools.Utils.Xml.Serialization;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Xml.Serialization
{
    /// <summary>
    /// Tests for ObjectXmlSerializer
    /// </summary>
    [TestFixture]
    public class ObjectXmlSerializerTest
    {
        

        /// <summary>
        /// Write a product to xml
        /// </summary>
        [Test]
        [Category("DataAccess")]
        public void WriteProduct()
        {
            string filePath = "xmltest.xml";
            Product product = new Product();
            product.ListPrice = 10.0m;
            product.Name = "testname";
            product.ProductID = 10;

            ObjectXmlSerializer<Product>.Save(product, filePath);
        }

        /// <summary>
        /// read a product from xml
        /// </summary>
        [Test]
        [Category("DataAccess")]
        public void ReadProduct()
        {
            string filePath = @"..\..\Utils\Xml\Serialization\TestData\SomeProduct.xml";
            Product product = ObjectXmlSerializer<Product>.Load(filePath);
            Assert.AreEqual(product.ProductID, 10);
            Assert.AreEqual(product.Name, "testname");
            Assert.AreEqual(product.ListPrice, 10.0m);
        }

        /// <summary>
        /// validate product vs xsd
        /// </summary>
        [Test]
        [Category("DataAccess")]
        public void ValidateProductXml()
        {
            string filePath = @"..\..\Utils\Xml\Serialization\TestData\SomeProduct.xml";
            //validate xmltest vs the schema xmltest.xsd which was created based on the xmltest.xml
            XmlValidator xmlValidate = new XmlValidator(filePath, @"..\..\Utils\Xml\Serialization\TestData\Product.xsd");
            Assert.AreEqual(xmlValidate.ValidateXmlFile(), false);
        }
    }
}