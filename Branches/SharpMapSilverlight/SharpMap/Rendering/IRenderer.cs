using System;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public interface IRenderer
    {
        void Render(IView view, IProvider DataSource, Func<IFeature, IStyle> getStyle, ICoordinateTransformation CoordinateTransformation);
    }
}
