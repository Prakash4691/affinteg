using Microsoft.Xrm.Sdk;
using System;
using System.Globalization;

namespace custom_api_plugin
{
    internal static class CustomApiRequestParameterReader
    {
        public static string GetRequiredStringParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The custom API parameter '{0}' is required.",
                        parameterName));
            }

            if (!(context.InputParameters[parameterName] is string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The custom API parameter '{0}' must contain a non-empty string value.",
                        parameterName));
            }

            return value.Trim();
        }

        public static string GetOptionalStringParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                return null;
            }

            if (!(context.InputParameters[parameterName] is string value))
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The custom API parameter '{0}' must contain a string value when provided.",
                        parameterName));
            }

            string trimmedValue = value.Trim();
            return trimmedValue.Length == 0 ? null : trimmedValue;
        }

        public static decimal? GetOptionalDecimalParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                return null;
            }

            object value = context.InputParameters[parameterName];

            if (value is decimal decimalValue)
            {
                return decimalValue;
            }

            if (value is double doubleValue)
            {
                return Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is long longValue)
            {
                return longValue;
            }

            if (value is Money money)
            {
                return money.Value;
            }

            if (value is string stringValue)
            {
                string trimmedValue = stringValue.Trim();
                if (trimmedValue.Length == 0)
                {
                    return null;
                }

                if (decimal.TryParse(
                    trimmedValue,
                    NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                    CultureInfo.InvariantCulture,
                    out decimal parsedValue))
                {
                    return parsedValue;
                }
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The custom API parameter '{0}' must be a numeric value when provided.",
                    parameterName));
        }

        public static Money GetRequiredMoneyParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The custom API parameter '{0}' is required.",
                        parameterName));
            }

            object value = context.InputParameters[parameterName];

            if (value is Money money)
            {
                return money;
            }

            if (value is decimal decimalValue)
            {
                return new Money(decimalValue);
            }

            if (value is double doubleValue)
            {
                return new Money(Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture));
            }

            if (value is int intValue)
            {
                return new Money(intValue);
            }

            if (value is long longValue)
            {
                return new Money(longValue);
            }

            if (value is string stringValue
                && decimal.TryParse(
                    stringValue,
                    NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                    CultureInfo.InvariantCulture,
                    out decimal parsedValue))
            {
                return new Money(parsedValue);
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The custom API parameter '{0}' must be a numeric value.",
                    parameterName));
        }

        public static DateTime GetRequiredDateParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The custom API parameter '{0}' is required.",
                        parameterName));
            }

            object value = context.InputParameters[parameterName];
            if (CustomApiDateParser.TryParseDate(value, out DateTime parsedDate))
            {
                return parsedDate;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The custom API parameter '{0}' must contain a valid date value.",
                    parameterName));
        }

        public static DateTime? GetOptionalDateParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                return null;
            }

            object value = context.InputParameters[parameterName];
            if (value is string stringValue)
            {
                string trimmedValue = stringValue.Trim();
                if (trimmedValue.Length == 0)
                {
                    return null;
                }
            }

            if (CustomApiDateParser.TryParseDate(value, out DateTime parsedDate))
            {
                return parsedDate;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The custom API parameter '{0}' must contain a valid date value when provided.",
                    parameterName));
        }

        public static int GetRequiredIntegerParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The custom API parameter '{0}' is required.",
                        parameterName));
            }

            object value = context.InputParameters[parameterName];
            if (value is int intValue)
            {
                return intValue;
            }

            if (value is OptionSetValue optionSetValue)
            {
                return optionSetValue.Value;
            }

            if (value is long longValue
                && longValue >= int.MinValue
                && longValue <= int.MaxValue)
            {
                return Convert.ToInt32(longValue, CultureInfo.InvariantCulture);
            }

            if (value is string stringValue
                && int.TryParse(
                    stringValue.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsedValue))
            {
                return parsedValue;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The custom API parameter '{0}' must be an integer value.",
                    parameterName));
        }

        public static int? GetOptionalIntegerParameter(IPluginExecutionContext context, string parameterName)
        {
            if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
            {
                return null;
            }

            object value = context.InputParameters[parameterName];
            if (value is int intValue)
            {
                return intValue;
            }

            if (value is OptionSetValue optionSetValue)
            {
                return optionSetValue.Value;
            }

            if (value is long longValue
                && longValue >= int.MinValue
                && longValue <= int.MaxValue)
            {
                return Convert.ToInt32(longValue, CultureInfo.InvariantCulture);
            }

            if (value is string stringValue)
            {
                string trimmedValue = stringValue.Trim();
                if (trimmedValue.Length == 0)
                {
                    return null;
                }

                if (int.TryParse(
                    trimmedValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsedValue))
                {
                    return parsedValue;
                }
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The custom API parameter '{0}' must be an integer value when provided.",
                    parameterName));
        }
}
}
