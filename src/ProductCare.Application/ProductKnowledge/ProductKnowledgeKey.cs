using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ProductCare.Application.ProductKnowledge
{
    public static class ProductKnowledgeKey
    {
        private static readonly Regex InvalidCharacters = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Create(string manufacturer, string modelNumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
            ArgumentException.ThrowIfNullOrWhiteSpace(modelNumber);

            string value = $"{manufacturer.Trim()}-{modelNumber.Trim()}".ToLowerInvariant();
            string key = InvalidCharacters.Replace(value, "-").Trim('-');

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Manufacturer and model number do not produce a valid product knowledge key.");
            }

            return key;
        }
    }
}
