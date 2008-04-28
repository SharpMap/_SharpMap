using System.Drawing.Drawing2D;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
namespace SharpMap.Layers
{
    public interface IVectorLayer
         : IDataLayer,
         ITransformableLayer,
         IStyleable<IVectorStyle>,
         IThemeable<IVectorStyle>
    {
        bool ClippingEnabled { get; set; }
        SmoothingMode SmoothingMode { get; set; }
    }
}
