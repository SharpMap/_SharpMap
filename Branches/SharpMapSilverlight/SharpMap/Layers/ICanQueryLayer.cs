using SharpMap.Data;
using SharpMap.Geometries;

namespace SharpMap.Layers
{
    public interface IQueryLayer
    {
        /// <summary>
        /// Returns the data associated with all the geometries that are intersected by 'geom'
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        IFeatures GetFeatures(BoundingBox box);
    }
}