using System;

namespace DelftTools.Utils.Validation
{
    /// <summary>
    /// this definition based on Microsoft.Practices.EnterpriseLibrary.Validation.ValidationResult
    /// </summary>
    public interface IValidationResult
    {
        /// <summary>
        /// This is a name that describes the location of the validation result. It contains the name of the member 
        /// that is associated with the validator.  It is null if the validator is defined at the type level.
        /// </summary>
        string Key { get; }

        ///// <summary>
        ///// This is the object that was validated. 
        ///// </summary>
        //Object Object { get; }
	
        /// <summary>
        /// This is a message that describes the validation failure.
        /// </summary>
        string Message { get; }

        ///// <summary>
        ///// This is a value that is supplied by the user that describes the result, usually for the purposes of 
        ///// categorization or filtering. The tag can be supplied through the constructor but it is typically 
        ///// set either by using a property in the validation attributes or with configuration.
        ///// </summary>
        //string Tag { get; }

        /// <summary>
        /// This is the object to which the validation rule was applied.
        /// </summary>
        Object Target { get; }
        
        ///// <summary>
        ///// This is the validator that performed the validation that failed.
        ///// </summary>
        //IValidator Validator { get; }
    }
}
