namespace DelftTools.Utils.Validation
{
    public class Validator
    {
        public static IValidationResults Validate(object target)
        {
            if (target is IValidatable)
            {
                return new ValidationResults((IValidatable)target);
            }
            // Or throw an error or return null?
            return new ValidationResults();
        }
    }
}
