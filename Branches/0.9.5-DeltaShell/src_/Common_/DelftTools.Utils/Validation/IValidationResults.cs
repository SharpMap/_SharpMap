using System.Collections.Generic;

namespace DelftTools.Utils.Validation
{
    public interface IValidationResults : IEnumerable<IValidationResult>
    {
        /// <summary>
        /// This property returns true if the validation passed and returns false if the validation failed.
        /// </summary>
        bool IsValid { get; }
    }
}
