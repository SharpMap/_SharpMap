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
        void Render(IView view, Map map);
    }
}
