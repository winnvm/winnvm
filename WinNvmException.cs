using System;

namespace WinNvm
{
    internal class WinNvmException : Exception
    {
        public WinNvmException(string message) : base(message)
        {
        }

        public WinNvmException(string message,params object[] args): base(string.Format(message, args))
        {
        }
    }
}