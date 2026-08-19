using System;

namespace SimaiSharp.Commands
{
    public abstract class Command
    {
        public abstract void Deserialize(ReadOnlySpan<byte> chartSpan);
    }
}
