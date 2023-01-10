using System;

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
            /// Class representing the main headers of the OML, allowing value modifications.
            /// </summary>
            /// <param name="oml">OML instance from which the header belongs to.</param>
            public OmlHeader(Oml oml)
            {
                object? localInstance = AssemblyUtility.GetInstanceField<object>(oml._instance, "Header");
                if (localInstance == null)
                {
                    throw new Exception("Unable to get OML headers. Null returned.");
                }
                _instance = localInstance;
            }

            /// <summary>
            /// Activation code header (read-only).
            /// </summary>
            [OmlHeader(IsReadOnly = true)]
            public string ActivationCode
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<string>(_instance, "ActivationCode") ?? string.Empty;
                }
            }

            /// <summary>
            /// Module name header.
            /// </summary>
            [OmlHeader]
            public string Name
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<string>(_instance, "Name") ?? string.Empty;
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "Name", value);
                }
            }

            /// <summary>
            /// Module description header.
            /// </summary>
            [OmlHeader]
            public string Description
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<string>(_instance, "Description") ?? string.Empty;
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "Description", value);
                }
            }

            /// <summary>
            /// Module type.
            /// </summary>
            [OmlHeader]
            public string Type
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<string>(_instance, "ESpaceType") ?? string.Empty;
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "ESpaceType", value);
                }
            }

            /// <summary>
            /// Header representing the last time the module was modified.
            /// </summary>
            [OmlHeader]
            public DateTime LastModifiedUTC
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<DateTime>(_instance, "LastModifiedUTC");
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "LastModifiedUTC", value);
                }
            }

            /// <summary>
            /// Whether the module must open in recovery mode.
            /// </summary>
            [OmlHeader]
            public bool NeedsRecover
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<bool>(_instance, "NeedsRecover");
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "NeedsRecover", value);
                }
            }

            /// <summary>
            /// Module signature header (read-only).
            /// </summary>
            [OmlHeader(IsReadOnly = true)]
            public string Signature
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<string>(_instance, "Signature") ?? string.Empty;
                }
            }

            /// <summary>
            /// Module service studio version header.
            /// </summary>
            [OmlHeader]
            public Version? Version
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<Version>(_instance, "Version");
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "Version", value);
                }
            }

            /// <summary>
            /// Header containing the previous service studio version before the last upgrade. 
            /// </summary>
            [OmlHeader]
            public Version? LastUpgradeVersion
            {
                get
                {
                    return AssemblyUtility.GetInstanceField<Version>(_instance, "LastUpgradeVersion");
                }
                set
                {
                    AssemblyUtility.SetInstanceField(_instance, "LastUpgradeVersion", value);
                }
            }

            /// <summary>
            /// Custom attribute for OML header parameters.
            /// </summary>
            public sealed class OmlHeaderAttribute : Attribute
            {
                /// <summary>
                /// Whether the attribute is read-only.
                /// </summary>
                public bool IsReadOnly;
            }
        }
    }
}
