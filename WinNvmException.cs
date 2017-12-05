using System;

namespace WinNvm
{
    internal class WinNvmException : Exception
    {
        public WinNvmException(string message) : base(message)
        {
        }
    }
}