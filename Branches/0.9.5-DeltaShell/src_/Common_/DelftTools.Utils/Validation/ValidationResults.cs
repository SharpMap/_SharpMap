using System.Collections;
using System.Collections.Generic;

namespace DelftTools.Utils.Validation
{
    class ValidationResults : IValidationResults
    {
        readonly IList<IValidationResult> validationResults = new List<IValidationResult>();

        /// <summary>
        /// empty validation results
        /// </summary>
        public ValidationResults()
        {
        }

        public ValidationResults(IValidatable target)
        {
            foreach (var result in target.Validate())
            {
                validationResults.Add(result);
            }
        }

        public IEnumerator<IValidationResult> GetEnumerator()
        {
            return validationResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsValid
        {
            get { return (validationResults.Count == 0); }
        }
    }
}
