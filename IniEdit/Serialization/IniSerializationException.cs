namespace IniEdit.Serialization
{
    /// <summary>
    /// The exception that is thrown when an error occurs during INI serialization or deserialization.
    /// </summary>
    public sealed class IniSerializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IniSerializationException"/> class.
        /// </summary>
        public IniSerializationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniSerializationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IniSerializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniSerializationException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IniSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
