using System.Runtime.Serialization;

namespace Application
{
    [Serializable]
    public class ApplicationException : Exception
    {
        public ApplicationException()
        {
        }

        public ApplicationException(string? message) : base(message)
        {
        }

        public ApplicationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ApplicationException(SerializationInfo serializationInfo, StreamingContext streamingContext) 
            : base(serializationInfo, streamingContext)
        {

        }
    }
}
