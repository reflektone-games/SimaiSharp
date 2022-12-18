namespace SimaiSharp.Internal
{
	internal struct TimingChange
	{
		public float tempo;
		public float subdivisions;

		private float SecondsPerBar  => 60f / tempo;
		public  float SecondsPerBeat => tempo <= 0 ? 0 : SecondsPerBar / (subdivisions / 4);

		public void ExplicitOverride(float value)
		{
			tempo        = 60f / value;
			subdivisions = 4;
		}

		public TimingChange(float tempo, float subdivisions = 4)
		{
			this.tempo        = tempo;
			this.subdivisions = subdivisions;
		}
	}
}