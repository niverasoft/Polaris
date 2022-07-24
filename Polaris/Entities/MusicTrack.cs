using System;

namespace Polaris.Entities
{
    public class MusicTrack
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public TimeSpan Duration { get; set; }
        public string URL { get; set; }
    }
}
