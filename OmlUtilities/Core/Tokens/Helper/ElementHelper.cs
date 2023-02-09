using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens.Helper
{
    public static class ElementHelper
    {
        public readonly static DateTime NullDate = new DateTime(1900, 1, 1, 0, 0, 0);

        public static DateTime ParseDateTime(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                DateTime dt;
                if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    return dt;
                }
            }
            return NullDate;
        }

        public static T GetAttribute<T>(XAttribute attribute)
        {
            object result = null;
            if (typeof(T).Equals(typeof(string)))
            {
                result = (string)attribute ?? "";
            }
            else if (typeof(T).Equals(typeof(DateTime)))
            {
                result = ParseDateTime((string)attribute);
            }
            else if (typeof(T).Equals(typeof(int)))
            {
                result = int.Parse((string)attribute);
            }
            else if (typeof(T).Equals(typeof(int?)))
            {
                int localResult;
                if (int.TryParse((string)attribute, out localResult))
                {
                    result = localResult;
                }
            }
            else if (typeof(T).Equals(typeof(long)))
            {
                result = long.Parse((string)attribute);
            }
            else if (typeof(T).Equals(typeof(long?)))
            {
                long localResult;
                if (long.TryParse((string)attribute, out localResult))
                {
                    result = localResult;
                }
            }
            else if (typeof(T).Equals(typeof(decimal)))
            {
                result = decimal.Parse((string)attribute);
            }
            else if (typeof(T).Equals(typeof(decimal?)))
            {
                decimal localResult;
                if (decimal.TryParse((string)attribute, out localResult))
                {
                    result = localResult;
                }
            }
            else if (typeof(T).Equals(typeof(bool)))
            {
                result = ((string)attribute ?? "").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            else
            {
                throw new OmlException($"Cannot convert attribute value to type {typeof(T).Name}.");
            }
            return (T)result;
        }

        public static T GetAttribute<T>(XElement xml, string attributeName)
        {
            if (xml == null || string.IsNullOrEmpty(attributeName))
            {
                object result;
                if (typeof(T).Equals(typeof(string)))
                {
                    result = "";
                }
                else if (typeof(T).Equals(typeof(DateTime)))
                {
                    result = NullDate;
                }
                else if (typeof(T).Equals(typeof(bool)))
                {
                    result = false;
                }
                else if (Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    result = null;
                }
                else
                {
                    throw new OmlException($"Cannot convert attribute value to type {typeof(T).Name}.");
                }
                return (T)result;
            }
            return GetAttribute<T>(xml.Attribute(attributeName));
        }

        public static bool TestAttribute(XAttribute attribute, string value, bool isCaseSensitive = true)
        {
            if (value == null)
            {
                value = "";
            }
            string attrValue = (string)attribute ?? "";
            return value.Equals(attrValue, isCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool TestAttribute(XElement xml, string attributeName, string value, bool isCaseSensitive = true)
        {
            return TestAttribute(xml.Attribute(attributeName), value, isCaseSensitive);
        }
    }
}
