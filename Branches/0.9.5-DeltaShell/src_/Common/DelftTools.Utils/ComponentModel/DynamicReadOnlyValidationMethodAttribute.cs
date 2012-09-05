using System;

namespace DelftTools.Utils.ComponentModel
{
    /// <summary>
    /// Used to mark method as a validation method used to check if property value can be set.
    /// <seealso cref="DynamicReadOnlyAttribute"/>
    /// </summary>
    public class DynamicReadOnlyValidationMethodAttribute : Attribute
    {
    }
}
