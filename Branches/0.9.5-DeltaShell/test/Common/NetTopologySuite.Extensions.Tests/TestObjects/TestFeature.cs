using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace NetTopologySuite.Extensions.Tests.TestObjects
{
    //Just a subclass
    class TestFeatureSubClass : TestFeature
    {

    }

    class TestFeature : Unique<long>, IFeature
    {
        [FeatureAttribute]
        public string Name { get; set; }

        [FeatureAttribute(DisplayName = "Kees")]
        public string Other { get; set; }
        
        public object Clone()
        {
            throw new NotImplementedException();
        }

        public IGeometry Geometry
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IFeatureAttributeCollection Attributes
        {
            get; set; }
    }
}
