using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace custom_api_plugin
{
    internal sealed class OpportunityAttributeValueResolver
    {
        private readonly IOrganizationService service;
        private readonly IDictionary<string, AttributeMetadata> attributeMetadataCache;

        public OpportunityAttributeValueResolver(IOrganizationService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            attributeMetadataCache = new Dictionary<string, AttributeMetadata>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetAttribute(Entity entity, string attributeName, object rawValue)
        {
            entity[attributeName] = Resolve(entity.LogicalName, attributeName, rawValue);
        }

        private object Resolve(string entityLogicalName, string attributeName, object rawValue)
        {
            AttributeMetadata attributeMetadata = GetAttributeMetadata(entityLogicalName, attributeName);
            if (!attributeMetadata.AttributeType.HasValue)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' does not expose a supported metadata type.",
                        attributeName,
                        entityLogicalName));
            }

            switch (attributeMetadata.AttributeType.Value)
            {
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                case AttributeTypeCode.EntityName:
                    return GetStringValue(rawValue, entityLogicalName, attributeName);

                case AttributeTypeCode.Boolean:
                    return GetBooleanValue(
                        rawValue,
                        entityLogicalName,
                        attributeName,
                        (BooleanAttributeMetadata)attributeMetadata);

                case AttributeTypeCode.Integer:
                    return GetIntegerValue(rawValue, entityLogicalName, attributeName);

                case AttributeTypeCode.BigInt:
                    return GetLongValue(rawValue, entityLogicalName, attributeName);

                case AttributeTypeCode.Decimal:
                    return GetDecimalValue(rawValue, entityLogicalName, attributeName);

                case AttributeTypeCode.Double:
                    return GetDoubleValue(rawValue, entityLogicalName, attributeName);

                case AttributeTypeCode.Money:
                    return new Money(GetDecimalValue(rawValue, entityLogicalName, attributeName));

                case AttributeTypeCode.DateTime:
                    return GetDateValue(rawValue, entityLogicalName, attributeName);

                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    return ResolveOptionSetValue(
                        rawValue,
                        entityLogicalName,
                        attributeName,
                        (EnumAttributeMetadata)attributeMetadata);

                default:
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The attribute '{0}' on entity '{1}' is not supported by the opportunity update contract.",
                            attributeName,
                            entityLogicalName));
            }
        }

        private AttributeMetadata GetAttributeMetadata(string entityLogicalName, string attributeName)
        {
            string cacheKey = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                entityLogicalName,
                attributeName);

            if (attributeMetadataCache.TryGetValue(cacheKey, out AttributeMetadata attributeMetadata))
            {
                return attributeMetadata;
            }

            RetrieveAttributeRequest request = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };

            RetrieveAttributeResponse response =
                (RetrieveAttributeResponse)service.Execute(request);
            attributeMetadata = response.AttributeMetadata;
            attributeMetadataCache[cacheKey] = attributeMetadata;
            return attributeMetadata;
        }

        private static string GetStringValue(
            object rawValue,
            string entityLogicalName,
            string attributeName)
        {
            if (rawValue is string stringValue)
            {
                return stringValue.Trim();
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The attribute '{0}' on entity '{1}' must be provided as a string value.",
                    attributeName,
                    entityLogicalName));
        }

        private static bool GetBooleanValue(
            object rawValue,
            string entityLogicalName,
            string attributeName,
            BooleanAttributeMetadata attributeMetadata)
        {
            if (rawValue is bool boolValue)
            {
                return boolValue;
            }

            if (!(rawValue is string stringValue))
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be provided as a boolean-compatible value.",
                        attributeName,
                        entityLogicalName));
            }

            string trimmedValue = stringValue.Trim();
            if (bool.TryParse(trimmedValue, out bool parsedValue))
            {
                return parsedValue;
            }

            if (string.Equals(trimmedValue, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(trimmedValue, "no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (MatchesOptionLabel(attributeMetadata.OptionSet.TrueOption, trimmedValue))
            {
                return true;
            }

            if (MatchesOptionLabel(attributeMetadata.OptionSet.FalseOption, trimmedValue))
            {
                return false;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The value '{0}' is not valid for boolean attribute '{1}' on entity '{2}'.",
                    trimmedValue,
                    attributeName,
                    entityLogicalName));
        }

        private static int GetIntegerValue(
            object rawValue,
            string entityLogicalName,
            string attributeName)
        {
            if (rawValue is int intValue)
            {
                return intValue;
            }

            if (rawValue is OptionSetValue optionSetValue)
            {
                return optionSetValue.Value;
            }

            if (rawValue is long longValue
                && longValue >= int.MinValue
                && longValue <= int.MaxValue)
            {
                return Convert.ToInt32(longValue, CultureInfo.InvariantCulture);
            }

            if (rawValue is decimal decimalValue
                && decimalValue >= int.MinValue
                && decimalValue <= int.MaxValue
                && decimal.Truncate(decimalValue) == decimalValue)
            {
                return decimal.ToInt32(decimalValue);
            }

            if (rawValue is string stringValue
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
                    "The attribute '{0}' on entity '{1}' must be an integer value.",
                    attributeName,
                    entityLogicalName));
        }

        private static long GetLongValue(
            object rawValue,
            string entityLogicalName,
            string attributeName)
        {
            if (rawValue is long longValue)
            {
                return longValue;
            }

            if (rawValue is int intValue)
            {
                return intValue;
            }

            if (rawValue is decimal decimalValue
                && decimal.Truncate(decimalValue) == decimalValue)
            {
                return decimal.ToInt64(decimalValue);
            }

            if (rawValue is string stringValue
                && long.TryParse(
                    stringValue.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long parsedValue))
            {
                return parsedValue;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The attribute '{0}' on entity '{1}' must be a whole-number value.",
                    attributeName,
                    entityLogicalName));
        }

        private static decimal GetDecimalValue(
            object rawValue,
            string entityLogicalName,
            string attributeName)
        {
            if (rawValue is decimal decimalValue)
            {
                return decimalValue;
            }

            if (rawValue is double doubleValue)
            {
                return Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
            }

            if (rawValue is int intValue)
            {
                return intValue;
            }

            if (rawValue is long longValue)
            {
                return longValue;
            }

            if (rawValue is Money moneyValue)
            {
                return moneyValue.Value;
            }

            if (rawValue is string stringValue
                && decimal.TryParse(
                    stringValue.Trim(),
                    NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                    CultureInfo.InvariantCulture,
                    out decimal parsedValue))
            {
                return parsedValue;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The attribute '{0}' on entity '{1}' must be a numeric value.",
                    attributeName,
                    entityLogicalName));
        }

        private static double GetDoubleValue(
            object rawValue,
            string entityLogicalName,
            string attributeName)
        {
            if (rawValue is double doubleValue)
            {
                return doubleValue;
            }

            if (rawValue is decimal decimalValue)
            {
                return Convert.ToDouble(decimalValue, CultureInfo.InvariantCulture);
            }

            if (rawValue is int intValue)
            {
                return intValue;
            }

            if (rawValue is long longValue)
            {
                return longValue;
            }

            if (rawValue is string stringValue
                && double.TryParse(
                    stringValue.Trim(),
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out double parsedValue))
            {
                return parsedValue;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The attribute '{0}' on entity '{1}' must be a numeric value.",
                    attributeName,
                    entityLogicalName));
        }

        private static DateTime GetDateValue(
            object rawValue,
            string entityLogicalName,
            string attributeName)
        {
            if (CustomApiDateParser.TryParseDate(rawValue, out DateTime parsedDate))
            {
                return parsedDate;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The attribute '{0}' on entity '{1}' must contain a valid date value.",
                    attributeName,
                    entityLogicalName));
        }

        private static OptionSetValue ResolveOptionSetValue(
            object rawValue,
            string entityLogicalName,
            string attributeName,
            EnumAttributeMetadata attributeMetadata)
        {
            if (rawValue is OptionSetValue optionSetValue)
            {
                return optionSetValue;
            }

            if (rawValue is int intValue)
            {
                return new OptionSetValue(intValue);
            }

            if (rawValue is string stringValue)
            {
                string trimmedValue = stringValue.Trim();
                if (int.TryParse(
                    trimmedValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsedIntegerValue))
                {
                    return new OptionSetValue(parsedIntegerValue);
                }

                foreach (OptionMetadata option in attributeMetadata.OptionSet.Options)
                {
                    if (option.Value.HasValue && MatchesOptionLabel(option, trimmedValue))
                    {
                        return new OptionSetValue(option.Value.Value);
                    }
                }
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The value for attribute '{0}' on entity '{1}' must be an integer or a valid option label.",
                    attributeName,
                    entityLogicalName));
        }

        private static bool MatchesOptionLabel(OptionMetadata option, string value)
        {
            if (option == null || option.Label == null)
            {
                return false;
            }

            if (option.Label.UserLocalizedLabel != null
                && string.Equals(
                    option.Label.UserLocalizedLabel.Label,
                    value,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (LocalizedLabel localizedLabel in option.Label.LocalizedLabels)
            {
                if (string.Equals(localizedLabel.Label, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
