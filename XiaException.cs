using System;

namespace XiaSaveFileLib
{
    public class XiaException : Exception
    {
        public XiaException()
        {

        }
        public XiaException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}