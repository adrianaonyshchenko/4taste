using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Services
{
    public sealed class RecipeScalingService : IRecipeScalingService
    {
        public const int MinServings = 1;
        public const int MaxServings = 50;

        private static readonly CultureInfo UaCulture = CultureInfo.GetCultureInfo("uk-UA");

        private static readonly Regex AmountRegex = new (
            @"^\s*(?<min>\d+(?:[.,]\d+)?)(?:\s*[-–—]\s*(?<max>\d+(?:[.,]\d+)?))?\s*(?<unit>.*)$",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, (Func<double, bool> ShouldConvert, double Factor, string TargetUnit)> UnitConversions = new ()
        {
            ["г"] = (v => v >= 1000.0, 0.001, "кг"),
            ["кг"] = (v => v < 1.0, 1000.0, "г"),
            ["мл"] = (v => v >= 1000.0, 0.001, "л"),
            ["л"] = (v => v < 1.0, 1000.0, "мл"),
        };

        public Result<string> ScaleAmount(string? rawAmount, int baseServings, int targetServings)
        {
            if (targetServings is < MinServings or > MaxServings)
            {
                return Result<string>.Failure($"Кількість порцій має бути від {MinServings} до {MaxServings}.");
            }

            if (string.IsNullOrWhiteSpace(rawAmount))
            {
                return Result<string>.Success(string.Empty);
            }

            int safeBase = baseServings > 0 ? baseServings : 1;
            string trimmed = rawAmount.Trim();

            if (safeBase == targetServings)
            {
                return Result<string>.Success(trimmed);
            }

            var match = AmountRegex.Match(trimmed);
            if (!match.Success || !TryParseNumber(match.Groups["min"].Value, out double minVal))
            {
                return Result<string>.Success(trimmed);
            }

            double scaleFactor = (double)targetServings / safeBase;
            string rawUnit = match.Groups["unit"].Value.Trim();

            double maxVal = 0;
            bool isRange = match.Groups["max"].Success && TryParseNumber(match.Groups["max"].Value, out maxVal);
            string formatted = isRange
                ? FormatRange(minVal * scaleFactor, maxVal * scaleFactor, rawUnit)
                : FormatSingle(minVal * scaleFactor, rawUnit);

            return Result<string>.Success(formatted);
        }

        private static string FormatSingle(double rawValue, string rawUnit)
        {
            var (scaled, unit) = NormalizeUnit(rawValue, rawUnit);
            double rounded = RoundByUnit(scaled, NormalizeUnitKey(unit));
            return AppendUnit(rounded.ToString("0.##", UaCulture), unit);
        }

        private static string FormatRange(double rawMin, double rawMax, string rawUnit)
        {
            var (_, targetUnit) = NormalizeUnit(rawMax, rawUnit);
            double conversionFactor = GetConversionFactor(NormalizeUnitKey(rawUnit), NormalizeUnitKey(targetUnit));

            string unitKey = NormalizeUnitKey(targetUnit);
            double minRounded = RoundByUnit(rawMin * conversionFactor, unitKey);
            double maxRounded = RoundByUnit(rawMax * conversionFactor, unitKey);

            string rangeText = $"{minRounded.ToString("0.##", UaCulture)}–{maxRounded.ToString("0.##", UaCulture)}";
            return AppendUnit(rangeText, targetUnit);
        }

        private static (double Value, string DisplayUnit) NormalizeUnit(double value, string rawUnit)
        {
            string key = NormalizeUnitKey(rawUnit);
            return UnitConversions.TryGetValue(key, out var rule) && rule.ShouldConvert(value)
                ? (value * rule.Factor, rule.TargetUnit)
                : (value, rawUnit);
        }

        private static double GetConversionFactor(string fromKey, string toKey) => (fromKey, toKey) switch
        {
            ("г", "кг") or("мл", "л") => 0.001,
            ("кг", "г") or("л", "мл") => 1000.0,
            _ => 1.0,
        };

        private static double RoundByUnit(double value, string unitKey) => unitKey switch
        {
            "кг" or "л" => Math.Round(value, 2),
            "шт" or "ст. л." or "ч. л." => RoundHalfStep(value),
            _ => value < 10.0 ? Math.Round(value, 1) : Math.Round(value, 0),
        };

        private static double RoundHalfStep(double value) => value switch
        {
            <= 0 => 0,
            < 0.5 => Math.Max(0.25, Math.Round(value * 4, MidpointRounding.AwayFromZero) / 4.0),
            _ => Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2.0,
        };

        private static string NormalizeUnitKey(string unit) => unit.Trim().TrimEnd('.').ToLowerInvariant() switch
        {
            "г" or "гр" => "г",
            "кг" => "кг",
            "мл" => "мл",
            "л" => "л",
            "шт" => "шт",
            "ст. л" or "ст.л" or "ст л" => "ст. л.",
            "ч. л" or "ч.л" or "ч л" => "ч. л.",
            var other => other,
        };

        private static bool TryParseNumber(string text, out double value) =>
            double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static string AppendUnit(string numberPart, string unit) =>
            string.IsNullOrWhiteSpace(unit) ? numberPart : $"{numberPart} {unit}";
    }
}