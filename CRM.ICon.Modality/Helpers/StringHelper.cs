// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CRM.ICon.Modality.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Converts a string to lowercase using ref parameter
        /// </summary>
        public static void NormalizeString(ref string? input)
        {
            input = NormalizeString(input);
        }

        /// <summary>
        /// Converts a string to lowercase and returns the result
        /// </summary>
        public static string NormalizeString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Uri.UnescapeDataString(input).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Converts all string properties and List&lt;string&gt; elements to lowercase
        /// </summary>
        public static T? NormalizeObject<T>(T request)
            where T : class
        {
            if (request == null)
                return null;

            var properties = typeof(T)
                .GetProperties()
                .Where(p => p.CanWrite && IsStringProperty(p.PropertyType));

            // Loop through each string property and convert values to lowercase
            foreach (var property in properties)
            {
                var value = property.GetValue(request);
                if (value == null)
                    continue;

                // Handle string properties
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    property.SetValue(request, NormalizeString(stringValue));
                }
                // Handle List<string> properties
                else if (value is List<string> stringList && stringList.Any())
                {
                    var normalizedList = stringList
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s.ToLowerInvariant())
                        .ToList();
                    property.SetValue(request, normalizedList);
                }
            }

            return request;
        }

        private static bool IsStringProperty(Type propertyType)
        {
            return propertyType == typeof(string) || propertyType == typeof(List<string>);
        }
    }
}
