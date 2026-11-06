using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Services
{
    public static class AllergyMatcher
    {
        private static readonly char[] KeywordSeparators = new[] { ',', ';', '\n', '\r' };
        private static readonly char[] CodeSeparators = new[] { ',', ';', ' ', '/', '\\', '|' };

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant();
        }

        public static HashSet<string> ResolveUserAllergenCodes(
            IEnumerable<string> userAllergens,
            IEnumerable<AllergenCatalogItem> catalog)
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalizedUserAllergens = userAllergens
                .Select(Normalize)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (normalizedUserAllergens.Count == 0)
            {
                return codes;
            }

            foreach (var item in catalog)
            {
                var itemCode = Normalize(item.Code);
                var itemName = Normalize(item.NameUk);
                var keywords = ExtractKeywords(item.Keywords);

                foreach (var userAllergy in normalizedUserAllergens)
                {
                    if (userAllergy.Equals(itemCode, StringComparison.OrdinalIgnoreCase) ||
                        userAllergy.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                        itemName.Contains(userAllergy, StringComparison.OrdinalIgnoreCase) ||
                        userAllergy.Contains(itemName, StringComparison.OrdinalIgnoreCase) ||
                        keywords.Any(k => k.Contains(userAllergy, StringComparison.OrdinalIgnoreCase) ||
                                          userAllergy.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        codes.Add(item.Code);
                    }
                }
            }

            return codes;
        }

        public static bool IsIngredientAllergenic(
            string ingredientName,
            IEnumerable<string> userAllergens,
            IEnumerable<AllergenCatalogItem> catalog)
        {
            var normalizedIngredient = Normalize(ingredientName);
            if (string.IsNullOrEmpty(normalizedIngredient))
            {
                return false;
            }

            var normalizedAllergens = userAllergens
                .Select(Normalize)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (normalizedAllergens.Count == 0)
            {
                return false;
            }

            if (normalizedAllergens.Any(al => normalizedIngredient.Contains(al, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var userCodes = ResolveUserAllergenCodes(normalizedAllergens, catalog);
            foreach (var item in catalog)
            {
                if (!userCodes.Contains(item.Code))
                {
                    continue;
                }

                var itemName = Normalize(item.NameUk);
                if (!string.IsNullOrEmpty(itemName) && normalizedIngredient.Contains(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var keywords = ExtractKeywords(item.Keywords);
                if (keywords.Any(k => !string.IsNullOrEmpty(k) && normalizedIngredient.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSubstituteSafe(
            IngredientSubstitute substitute,
            HashSet<string> userAllergenCodes,
            IEnumerable<string> userAllergens,
            IEnumerable<AllergenCatalogItem> catalog)
        {
            if (substitute is null)
            {
                return false;
            }

            if (!substitute.IsVerified)
            {
                return false;
            }

            var substituteCodes = ParseCodes(substitute.AllergenCodes);
            if (substituteCodes.Any(c => userAllergenCodes.Contains(c)))
            {
                return false;
            }

            var normOrig = Normalize(substitute.OriginalIngredientName);
            var normSubstitute = Normalize(substitute.SubstituteName);

            foreach (var item in catalog)
            {
                if (!userAllergenCodes.Contains(item.Code))
                {
                    continue;
                }

                if (normOrig == Normalize(item.Code) ||
                    normOrig == Normalize(item.NameUk) ||
                    ExtractKeywords(item.Keywords).Contains(normOrig))
                {
                    continue;
                }

                if (normSubstitute.Contains(Normalize(item.NameUk)) ||
                    ExtractKeywords(item.Keywords).Any(k => normSubstitute.Contains(k)))
                {
                    return false;
                }
            }

            return true;
        }

        public static (string CleanName, string CleanAmount) ExtractCleanNameAndAmount(string rawName, string rawAmount)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return (string.Empty, rawAmount ?? string.Empty);
            }

            rawName = rawName.Trim();
            rawAmount = rawAmount?.Trim() ?? string.Empty;

            var match = Regex.Match(
                rawName,
                @"^(.*?)(?:\s*[-–—:]\s*|\s*\()([\d\.,\s\-/]+(?:г|кг|мл|л|ст\.\s*л\.|ч\.\s*л\.|шт|ст|зубчик|дрібка|пакетик|склянк)[^\)]*)\)?$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string parsedName = match.Groups[1].Value.Trim();
                string embeddedAmount = match.Groups[2].Value.Trim();

                if (!string.IsNullOrEmpty(parsedName) && !string.IsNullOrEmpty(embeddedAmount))
                {
                    if (string.IsNullOrWhiteSpace(rawAmount) || rawAmount == "1 шт." || rawAmount == "1 шт")
                    {
                        return (parsedName, embeddedAmount);
                    }

                    return (parsedName, rawAmount);
                }
            }

            return (rawName, string.IsNullOrWhiteSpace(rawAmount) ? "1 шт." : rawAmount);
        }

        public static string CalculateSubstituteAmount(string originalAmount, double ratio)
        {
            if (string.IsNullOrWhiteSpace(originalAmount))
            {
                return "1 шт.";
            }

            originalAmount = originalAmount.Trim();
            if (Math.Abs(ratio - 1.0) < 0.001)
            {
                return originalAmount;
            }

            var rangeMatch = Regex.Match(originalAmount, @"^([\d\.,]+)\s*[-–]\s*([\d\.,]+)\s*(.*)$");
            if (rangeMatch.Success)
            {
                if (double.TryParse(rangeMatch.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double minVal) &&
                    double.TryParse(rangeMatch.Groups[2].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double maxVal))
                {
                    string minStr = (minVal * ratio).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                    string maxStr = (maxVal * ratio).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                    string unit = rangeMatch.Groups[3].Value.Trim();
                    return string.IsNullOrEmpty(unit) ? $"{minStr}-{maxStr}" : $"{minStr}-{maxStr} {unit}".Trim();
                }
            }

            var match = Regex.Match(originalAmount, @"^([\d\.,]+)\s*(.*)$");
            if (match.Success)
            {
                var numStr = match.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedVal))
                {
                    double calculated = parsedVal * ratio;
                    string formattedVal = calculated.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                    string unit = match.Groups[2].Value.Trim();
                    return string.IsNullOrEmpty(unit) ? formattedVal : $"{formattedVal} {unit}".Trim();
                }
            }

            return $"{originalAmount} (×{ratio:0.##})";
        }

        public static List<string> ExtractKeywords(string? keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords))
            {
                return new List<string>();
            }

            return keywords
                .Split(KeywordSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();
        }

        public static HashSet<string> ParseCodes(string? codes)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(codes))
            {
                return result;
            }

            foreach (var item in codes.Split(CodeSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                var clean = Normalize(item);
                if (!string.IsNullOrEmpty(clean))
                {
                    result.Add(clean);
                }
            }

            return result;
        }
    }
}
