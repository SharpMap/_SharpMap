using System;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;

namespace SharpMap.Layers
{
    public interface ILayerGroup : ILayer, IDisposable, INotifyCollectionChanged
    {
        IEventedList<ILayer> Layers { get; set; }
    }
}