using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Presentation.AspNet
{
    public interface IMapHandler<TWebMap, TOutput, TMapRequestConfig>
        where TWebMap : IWebMap<TMapRequestConfig, TOutput>
        where TMapRequestConfig : IMapRequestConfig
    {
    }
}
