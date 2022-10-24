using System;

namespace Polaris.Entities
{
    public class MusicTrack
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string AuthorUrl { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }    

        public TimeSpan Duration { get; set; }
    }
}
