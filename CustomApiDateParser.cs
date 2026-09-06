using System;
using System.Globalization;

namespace custom_api_plugin
{
    internal static class CustomApiDateParser
    {
        private static readonly string[] SupportedDateFormats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "o"
        };

        public static bool TryParseDate(object value, out DateTime parsedDate)
        {
            if (value is DateTime dateTimeValue)
            {
                parsedDate = dateTimeValue.Date;
                return true;
            }

            if (value is string stringValue
                && DateTime.TryParseExact(
                    stringValue.Trim(),
                    SupportedDateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out DateTime parsedDateValue))
            {
                parsedDate = parsedDateValue.Date;
                return true;
            }

            parsedDate = default(DateTime);
            return false;
        }

        public static bool TryParseDateTime(object value, out DateTime parsedDateTime)
        {
            if (value is DateTime dateTimeValue)
            {
                parsedDateTime = dateTimeValue.Date;
                return true;
            }

            if (value is string stringValue
                && DateTime.TryParseExact(
                    stringValue.Trim(),
                    SupportedDateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out DateTime parsedDateTimeValue))
            {
                parsedDateTime = parsedDateTimeValue.Date;
                return true;
            }

            parsedDateTime = default(DateTime);
            return false;
        }
    }
}
