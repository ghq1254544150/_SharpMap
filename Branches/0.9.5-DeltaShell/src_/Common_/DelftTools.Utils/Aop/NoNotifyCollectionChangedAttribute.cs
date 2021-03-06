using System;
using PostSharp.Extensibility;

namespace DelftTools.Utils.Aop
{
    /// <summary>
    /// Apply this attribute to properties you do not want to be intercepted
    /// by NotifyCollectionChangedAttribute. 
    /// </summary>
    [Serializable, MulticastAttributeUsage(MulticastTargets.Property)]
    public class NoNotifyCollectionChangedAttribute : Attribute
    {
    }
}
