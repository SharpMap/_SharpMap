using System;

namespace DelftTools.Utils.ComponentModel
{
    /// <summary>
    /// Marks property as a conditional read-only property. When this attribute is used on a property - a class containing that property
    /// must containg a single validation method (argument propertyName as string, returns bool) marked using [DynamicReadOnlyValidationMethod] 
    /// attribute.
    /// </summary>
    public class DynamicReadOnlyAttribute : Attribute
    {
    }
}
