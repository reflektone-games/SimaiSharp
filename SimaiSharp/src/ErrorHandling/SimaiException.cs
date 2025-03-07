using System;

namespace SimaiSharp.ErrorHandling
{
    public abstract class SimaiException : Exception
    {
        public int line;
        public int column;
    }
}
