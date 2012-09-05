namespace DelftTools.Utils.Validation
{
    public class ValidationResult : IValidationResult
    {
        public ValidationResult(object target, string key, string message)
        {
            Key = key;
            Target = target;
            Message = message;
        }

        public string Key { get; private set; }

        public string Message { get; private set; }

        public object Target { get; private set; }
    }
}
