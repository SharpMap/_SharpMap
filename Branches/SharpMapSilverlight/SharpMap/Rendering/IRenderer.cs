using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpMap;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.Data;

namespace SharpMap.Rendering
{
    public interface IRenderer
    {
        void Render(IProvider DataSource, Func<IFeature, IStyle> getStyle, ICoordinateTransformation CoordinateTransformation, IMapTransform mapTansform);
    }
}
