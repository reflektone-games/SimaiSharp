using System;
using System.Collections.Generic;
using SimaiSharp.Commands;
using SimaiSharp.Structures;
using SimaiSharp.Utilities;

namespace SimaiSharp
{
    public static class SimaiConvert
    {
        private static readonly Dictionary<int, Command> CommandMapping = new();

        static SimaiConvert()
        {
            AddCommand<HiSpeedCommand>("HS");
            AddCommand<SpeedMultiplierCommand>("SM");
            AddCommand<SpeedVariationCommand>("SV");
            AddCommand<NoteGroupCommand>("G");
            AddCommand<JumpCommand>(string.Empty);
        }

        public static SimaiChart Deserialize(ReadOnlySpan<byte> bytes) => SimaiDeserializer.Deserialize(bytes);

        public static void AddCommand<T>(string key) where T : Command, new()
        {
            var hashedKey = Hashing.ComputeHash(key);
            CommandMapping.Add(hashedKey, new T());
        }

        public static bool TryGetCommand(int hash, out Command command) =>
            CommandMapping.TryGetValue(hash, out command);

        public static bool TryGetCommand(ReadOnlySpan<byte> hash, out Command command) =>
            CommandMapping.TryGetValue(Hashing.ComputeHash(hash), out command);
    }
}
