using System;
using PostSharp.Extensibility;

namespace DelftTools.Utils.Aop.NotifyPropertyChanged
{
    /// <summary>
    /// Apply this attribute to properties you do not want to be intercepted
    /// by NotifyPropertyChangedAttribute. 
    /// </summary>
    [Serializable, MulticastAttributeUsage(MulticastTargets.Property)]
    public class NoNotifyPropertyChangedAttribute: Attribute
    {
    }
}