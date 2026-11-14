using System;

namespace WinNvm
{
    internal sealed class WinNvmException : Exception
    {
        public WinNvmException(string message) : base(message)
        {
        }

        public WinNvmException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
