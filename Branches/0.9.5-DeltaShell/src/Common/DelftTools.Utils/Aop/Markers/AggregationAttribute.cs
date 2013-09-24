namespace DelftTools.Utils.Aop
{
    using System;

    /// <summary>
    /// Identifies relation as an aggregation association (used by). By default properties and fields are considered compositions (owned by).
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Property)]
    public class AggregationAttribute : Attribute
    {
    }
}