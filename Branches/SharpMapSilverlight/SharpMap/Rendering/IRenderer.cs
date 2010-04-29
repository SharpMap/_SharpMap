using System;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Styles;
using SharpMap.Layers;

namespace SharpMap.Rendering
{
    public interface IRenderer
    {
        void RenderLayer(IView view, IProvider dataSource, Func<IFeature, IStyle> getStyle, ICoordinateTransformation CoordinateTransformation);
        void RenderLabelLayer(IView view, IProvider dataSource, LabelLayer labelLayer); //the layer itself should not be passed just a description of the style/theme
    }
}
