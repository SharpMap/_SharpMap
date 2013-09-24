using System;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// MoveToolFeature is an auxiliary class used by the movetool. This class can be removed
    /// when IFeature also inherits from IClonable. 
    /// Movetool can not create a copy of a feature since it lacks knowledge. Normally
    /// Toplogy rules clone features. Toplogy rules do have specific knowledge of features.
    /// </summary>
    public class MoveToolFeature : Unique<long>, IFeature
    {
        #region IFeature Members
        
        private IGeometry geometry;
        public IGeometry Geometry
        {
            get { return geometry; }
            set { geometry = value; }
        }
        public IFeatureAttributeCollection Attributes
        {
            get { throw new Exception("The method or operation is not implemented."); }
            set { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion

        public object Clone()
        {
            return new MoveToolFeature {Geometry = (IGeometry) Geometry.Clone()};
        }
    }
}
