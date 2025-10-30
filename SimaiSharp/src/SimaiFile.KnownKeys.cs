namespace SimaiSharp
{
    public sealed partial class SimaiFile
    {
        public const string IdKey     = "id";
        public const string ServerKey = "server";

        private const string MaidataKey     = "maidata";
        private const string TrackKey       = "track";
        private const string BackgroundKey  = "bg";
        private const string MovieKey       = "pv";
        private const string MovieKeyCommon = "mv";

        public const  string TitleKey       = "title";
        public const  string ArtistKey      = "artist";
        public const  string FreeMessageKey = "freemsg";
        public const  string CharterKey     = "des";
        private const string TagsKey        = "tags";
        private const string OffsetKey      = "first";

        private const string PreviewSeekKey    = "demo_seek";
        private const string PreviewStartKey   = "pvstart";
        private const string PreviewSeekEndKey = "pvend";
        private const string PreviewLengthKey  = "demo_len";

        // TODO)) Consider using source generation to register keywords
        // private partial string Penis { get; }
        //
        // private partial string Penis
        // {
        //     get => string.Empty;
        // }
    }
}
