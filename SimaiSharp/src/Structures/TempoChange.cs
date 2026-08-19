namespace SimaiSharp.Structures
{
    public struct TempoChange
    {
        public double time;
        public double tempo;
        public double subdivisions;

        /// <summary>
        /// Used in duration parsing.
        /// </summary>
        public double SecondsPerBar => tempo == 0 ? 0 : 60f / tempo;

        public double SecondsPerBeat => SecondsPerBar / ((subdivisions == 0 ? 4 : subdivisions) / 4);

        public bool IsValid => time >= 0;

        public void SetSeconds(double value)
        {
            tempo        = 60f / value;
            subdivisions = 4;
        }

        public static TempoChange invalid = new()
        {
            time         = -1,
            tempo        = 0,
            subdivisions = 0
        };
    }
}
