namespace SimaiSharp.Structures
{
    public struct SlideSegment
    {
        public readonly int startLocation;
        public readonly int midPoint;
        public readonly int endLocation;

        public SlideType slideType;

        public SlideSegment(int startLocation, int endLocation, int midPoint = 0x00)
        {
            this.startLocation = startLocation;
            this.endLocation   = endLocation;
            this.midPoint      = midPoint;

            slideType = SlideType.StraightLine;
        }
    }
}
