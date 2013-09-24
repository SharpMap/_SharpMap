using System;
using System.Collections.Generic;
using System.Linq;

namespace DelftTools.Utils.Validation
{
    public static class ValidationHelper
    {
        public static IList<ValidationIssue> ValidateDuplicateNames(IEnumerable<INameable> nameables, string typeNamePlural, object viewData, ValidationSeverity severity = ValidationSeverity.Error)
        {
            var issues = new List<ValidationIssue>();
            var nonUniqueNames = GetNonUniqueNames(nameables);

            foreach (var nonUniqueName in nonUniqueNames)
            {
                var first = nameables.First(n => n.Name == nonUniqueName);
                
                issues.Add(
                    new ValidationIssue(first, severity,
                                        String.Format("Several {0} with the same id exist", typeNamePlural),
                                        viewData));
            }
            return issues;
        }

        private static IEnumerable<string> GetNonUniqueNames(IEnumerable<INameable> nameables)
        {
            var names = nameables.Select(fc => fc.Name).ToList();
            var distinctNames = names.Distinct().ToList();

            if (names.Count != distinctNames.Count)
            {
                distinctNames.ForEach(id => names.Remove(id));
                return names;
            }
            return new string[0];
        }
    }
}