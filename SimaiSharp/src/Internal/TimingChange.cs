namespace SimaiSharp.Internal
{
	internal readonly struct TimingChange
	{
		public readonly float time;
		public readonly float tempo;
		public readonly float subdivisions;

		public float SecondsPerBeat => tempo <= 0 ? 0 : 60f / tempo / (subdivisions / 4);

		public TimingChange(float time, float tempo, float subdivisions = 4)
		{
			this.tempo        = tempo;
			this.subdivisions = subdivisions;
			this.time         = time;
		}
	}
}