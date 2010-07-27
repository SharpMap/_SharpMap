using System.Collections.Generic;

namespace DelftTools.Utils.Validation
{
    /// <summary>
    /// First QAD implemenation of validation, see
    /// http://wiki.deltares.nl/display/TOOLS/2009/04/22/Data+validation+how+can+we+implement+it
    /// 
    /// todo: implement using aspects (AOP or see Enterprise Library 4.1 - October 2008
    ///                               Introduction to the Validation Application Block)
    /// public class MyExample
    /// {
    ///   public static void Main()
    ///   {
    ///     Customer myCustomer = new Customer("A name that is too long");
    ///     ValidationResults r = Validation.Validate<Customer>(myCustomer);
    ///     if (!r.IsValid)
    ///     {
    ///       throw new InvalidOperationException("Validation error found.");
    ///     }
    ///   }
    /// }

    /// </summary>

    public interface IValidatable
    {
        IEnumerable<IValidationResult> Validate();
    }
}
