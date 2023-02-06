using System;
using System.Reflection;

namespace OmlUtilities.Core
{
    partial class Oml
    {
        public class OmlHeader
        {
            /// <summary>
            /// Assembly instance.
            /// </summary>
            protected object _instance;

            /// <summary>
            /// Name of the header.
            /// </summary>
            public string HeaderName { get; }

            /// <summary>
            /// Header instance type.
            /// </summary>
            public Type HeaderType { get; }

            /// <summary>
            /// Whether this header is read-only or writable.
            /// </summary>
            public bool IsReadOnly { get; }

            /// <summary>
            /// Class representing a header of the OML, allowing value modifications.
            /// </summary>
            /// <param name="oml">OML instance from which the header belongs to.</param>
            /// <param name="headerName">Name of the header.</param>
            /// <param name="headerType">Instance type of the header.</param>
            /// <param name="isReadOnly">Whether this header is read-only or writable.</param>
            public OmlHeader(Oml oml, string headerName, Type headerType, bool isReadOnly = false)
            {
                _instance = AssemblyUtility.GetInstanceField(oml._omlInstance, "Header");

                // Validate the provided type
                Type valueType = AssemblyUtility.GetInstanceField(_instance, headerName).GetType();
                if (!valueType.Equals(headerType) && !(headerType.Equals(typeof(string)) && valueType.IsEnum))
                {
                    throw new OmlException($"Type mismatch for header '{headerName}'. Expected '{valueType.Name}', got '{headerType.Name}'.");
                }

                HeaderName = headerName;
                HeaderType = headerType;
                IsReadOnly = isReadOnly;
            }

            /// <summary>
            /// Gets the value of this OML header.
            /// </summary>
            /// <typeparam name="T">Type to cast the OML header value to.</typeparam>
            /// <returns>Requested OML header value.</returns>
            public T GetValue<T>()
            {
                object value = AssemblyUtility.GetInstanceField(_instance, HeaderName);

                if (typeof(T).Equals(typeof(string)))
                {
                    if (value == null)
                    {
                        value = string.Empty;
                    }
                    else if (value is DateTime dateTime)
                    {
                        value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else if (value.GetType().IsEnum)
                    {
                        value = Enum.GetName(value.GetType(), value);
                    }
                    else
                    {
                        value = value.ToString();
                    }
                }

                return (T)value;
            }

            /// <summary>
            /// Sets the value of this OML header.
            /// </summary>
            /// <param name="value">Value to be set.</param>
            /// <exception cref="OmlException">Thrown if this header is read-only, or if the provided object type is not compatible with the header instance type.</exception>
            public void SetValue(object value)
            {
                if (IsReadOnly)
                {
                    throw new OmlException($"Cannot set header '{HeaderName}' because it's read-only.");
                }

                object oldValue = AssemblyUtility.GetInstanceField(_instance, HeaderName);
                Type type = oldValue.GetType();

                if (value == null || value.GetType().Equals(type))
                {
                    // Do nothing -- we'll just use value as-is
                }
                else if (value is string valueString)
                {
                    if (type.IsEnum)
                    {
                        if (!Enum.TryParse(type, valueString, false, out value))
                        {
                            throw new OmlException($"Header '{HeaderName}' was provided with an unknown value '{valueString}' for enumerator of type '{type.Name}'.");
                        }
                    }
                    else if (type.Equals(typeof(DateTime)))
                    {
                        value = DateTime.ParseExact(valueString, "yyyy-MM-dd HH:mm:ss", null);
                    }
                    else if (type.Equals(typeof(Version)))
                    {
                        value = Version.Parse(valueString);
                    }
                    else if (type.Equals(typeof(bool)))
                    {
                        value = valueString.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        throw new OmlException($"Header '{HeaderName}' was provided a string value that could not be converted to '{type.Name}'.");
                    }
                }
                else
                {
                    throw new OmlException($"Header '{HeaderName}' was provided a value with an incompatible type. Expected '{type.Name}', received '{value.GetType().Name}'.");
                }

                AssemblyUtility.SetInstanceField(_instance, HeaderName, value);
            }
        }
    }
}
