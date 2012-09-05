using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.Helpers;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Hydo.Helpers
{
    [TestFixture]
    public class CrossSectionValidatorTest
    {
        [Test]
        public void CrossSectionTypeTabulatedZWShouldNotBeTestedAndAlwayBeValid()
        {
            var crossSection = new CrossSectionDefinitionZW();
            Assert.IsTrue(CrossSectionValidator.IsFlowProfileValid(crossSection));
        }
    }
}
