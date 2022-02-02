using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace CodeDesignPlus.Redis.Test.Helpers.Extensions
{
    /// <summary>
    /// Methods extensions to DataAnnotations
    /// </summary>
    public static class DataAnnotationsExtensions
    {
        /// <summary>
        /// Validate the annotations in a class
        /// </summary>
        /// <typeparam name="T">Type object to validate</typeparam>
        /// <param name="data">Object to validate</param>
        /// <returns>Return a list with the result of the validations</returns>
        public static IList<ValidationResult> Validate<T>(this T data)
        {
            var results = new List<ValidationResult>();

            var validationContext = new ValidationContext(data, null, null);

            Validator.TryValidateObject(data, validationContext, results, true);

            if (data is IValidatableObject)
                (data as IValidatableObject).Validate(validationContext);

            return results;
        }
    }
}
