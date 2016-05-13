﻿using System;
using System.Runtime.Serialization;

namespace Gum.Translator.Text2AST.Parsing
{
    [Serializable]
    internal class ParsingPreUnaryOperationFailedException : Exception
    {
        public ParsingPreUnaryOperationFailedException()
        {
        }

        public ParsingPreUnaryOperationFailedException(string message) : base(message)
        {
        }

        public ParsingPreUnaryOperationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ParsingPreUnaryOperationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}