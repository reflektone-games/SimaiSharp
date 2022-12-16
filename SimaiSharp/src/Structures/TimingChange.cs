namespace SimaiSharp.Structures
{
	public readonly struct TimingChange
	{
		public readonly float time;
		public readonly float tempo;
		public readonly float subdivisions;

		public float SecondsPerBeat => 60f / tempo / (subdivisions / 4);

		public TimingChange(float time, float tempo, float subdivisions)
		{
			this.tempo        = tempo;
			this.subdivisions = subdivisions;
			this.time         = time;
		}
	}
}