using System;

namespace OmlUtilities.Core
{
    class OmlException : Exception
    {
        /// <summary>
        /// Exception related to the Oml and OmlUtilities classes.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public OmlException(string message) : base(message)
        {
            // Use default constructor
        }

        /// <summary>
        /// Exception related to the Oml and OmlUtilities classes.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        public OmlException(string message, Exception innerException) : base(message, innerException)
        {
            // Use default constructor
        }
    }
}
