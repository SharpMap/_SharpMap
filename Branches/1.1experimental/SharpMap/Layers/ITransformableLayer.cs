using SharpMap.CoordinateSystems.Transformations;

namespace SharpMap.Layers
{
    public interface ITransformableLayer
    {
        ICoordinateTransformation CoordinateTransformation { get; set; }
    }
}
