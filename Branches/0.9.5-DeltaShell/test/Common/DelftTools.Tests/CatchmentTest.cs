using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace DelftTools.Tests
{
    [TestFixture]
    public class CatchmentTest
    {
        [Test]
        public void DefaultCatchment()
        {
            var catchment = new Catchment();
            Assert.IsNotNull(catchment);
        }

        [Test]
        public void CloneWithNoGeometry()
        {
            var catchment = new Catchment();
            var clone = (Catchment)catchment.Clone();

            Assert.IsNotNull(clone);
        }
        
        [Test]
        public void DefaultGeometryForArea()
        {
            var catchment = new Catchment { IsGeometryDerivedFromAreaSize = true };
            var expected = 500;
            catchment.SetAreaSize(expected);

            Assert.AreEqual(expected, catchment.Geometry.Area, 0.01);
            Assert.AreEqual(expected, catchment.AreaSize, 0.01);
        }

        [Test]
        public void DefaultGeometryForLargeArea()
        {
            var catchment = new Catchment {IsGeometryDerivedFromAreaSize = true};
            var expected = 9000;
            catchment.SetAreaSize(expected);

            Assert.AreEqual(expected, catchment.Geometry.Area, 0.01);
            Assert.AreEqual(expected, catchment.AreaSize, 0.01);
        }

        [Test]
        public void DefaultGeometryForEmptyArea()
        {
            var catchment = new Catchment { IsGeometryDerivedFromAreaSize = true };
            var expected = 0;
            catchment.SetAreaSize(expected);

            Assert.AreEqual(expected, catchment.Geometry.Area, 1.0);
            Assert.AreEqual(expected, catchment.AreaSize);
        }

        [Test]
        public void Clone()
        {
            var catchment = new Catchment();
            catchment.Geometry = new Point(15d, 15d);
            var clone = (Catchment)catchment.Clone();

            Assert.AreEqual(catchment.Geometry, clone.Geometry);
            Assert.AreNotSame(catchment.Geometry, clone.Geometry);
            Assert.AreEqual(catchment.Name, clone.Name);
        }

        [Test]
        public void CopyFrom()
        {
            var catchment1 = new Catchment();
            var catchment2 = new Catchment
                                 {
                                     Name = "Aapje",
                                     Geometry =
                                         new Polygon(
                                         new LinearRing(new ICoordinate[]
                                                            {
                                                                new Coordinate(10d, 10d), new Coordinate(20d, 10d),
                                                                new Coordinate(15d, 15d), new Coordinate(10d, 10d)
                                                            })),
                                     Description = "Komt uit de mouw",
                                     Network = new HydroNetwork()
                                 };
            catchment2.Attributes.Add("gras", 15);

            catchment1.CopyFrom(catchment2);

            Assert.AreNotEqual(catchment1.Geometry, catchment2.Geometry);
            Assert.AreNotEqual(catchment1.Name, catchment2.Name);
            Assert.AreEqual(catchment1.Attributes, catchment2.Attributes);
            Assert.AreEqual(catchment1.Description, catchment2.Description);
            Assert.AreSame(catchment1.Network, catchment2.Network);
            Assert.AreNotSame(catchment1.Attributes, catchment2.Attributes);
        }
    }
}
