using System;

namespace PocketLib
{
    [Serializable]
    public class PocketException : Exception
    {
        public PocketException() { }
        public PocketException(string message, Exception inner) : base(message, inner) { }
        protected PocketException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
