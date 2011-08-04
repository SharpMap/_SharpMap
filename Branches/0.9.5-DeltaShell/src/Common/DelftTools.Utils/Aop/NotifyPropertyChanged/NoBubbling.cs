using System;
using PostSharp.Extensibility;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    [Serializable, MulticastAttributeUsage(MulticastTargets.Field)]
    public class NoBubbling:Attribute{}
}