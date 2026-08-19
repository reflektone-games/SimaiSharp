namespace SimaiSharp
{
    internal static class Constants
    {
        public const byte SpaceChar                     = (byte)' ';
        public const byte NullChar                      = (byte)'\0';
        public const byte CarriageReturnChar            = (byte)'\r';
        public const byte LineFeedChar                  = (byte)'\n';
        public const byte TempoBracketOpenChar          = (byte)'(';
        public const byte TempoBracketCloseChar         = (byte)')';
        public const byte SubdivisionOpenChar           = (byte)'{';
        public const byte SubdivisionCloseChar          = (byte)'}';
        public const byte DurationOpenChar              = (byte)'[';
        public const byte DurationCloseChar             = (byte)']';
        public const byte HashChar                      = (byte)'#';
        public const byte ColonChar                     = (byte)':';
        public const byte SingleLineCommentChar         = (byte)'|';
        public const byte TimeStepChar                  = (byte)',';
        public const byte SeparatorChar                 = (byte)'/';
        public const byte ForceEachChar                 = (byte)'0';
        public const byte SplitFrameChar                = (byte)'`';
        public const byte ButtonCharStart               = (byte)'1';
        public const byte ButtonCharEnd                 = (byte)'8';
        public const byte SensorCharStart               = (byte)'A';
        public const byte SensorCharEndOrEof            = (byte)'E';
        public const byte FireworkChar                  = (byte)'f';
        public const byte BreakChar                     = (byte)'b';
        public const byte ExChar                        = (byte)'x';
        public const byte MineChar                      = (byte)'m';
        public const byte HoldChar                      = (byte)'h';
        public const byte TapRemovedSlideChar           = (byte)'?';
        public const byte SuddenSlideChar               = (byte)'!';
        public const byte ForceStarChar                 = (byte)'$';
        public const byte ForceNonStarChar              = (byte)'@';
        public const byte NewSlideOrCommandArgumentChar = (byte)'*';
        public const byte StraightLineChar              = (byte)'-';
        public const byte RingRightOrCommandCloseChar   = (byte)'>';
        public const byte RingLeftOrCommandOpenChar     = (byte)'<';
        public const byte RingAutoShortChar             = (byte)'^';
        public const byte CurveCwChar                   = (byte)'q';
        public const byte CurveCcwChar                  = (byte)'p';
        public const byte FoldChar                      = (byte)'v';
        public const byte EdgeFoldChar                  = (byte)'V';
        public const byte ZigZagSChar                   = (byte)'s';
        public const byte ZigZagZChar                   = (byte)'z';
        public const byte FanChar                       = (byte)'w';
    }
}
