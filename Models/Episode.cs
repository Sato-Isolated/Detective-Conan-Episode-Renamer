using System.Collections.Generic;

namespace DetectiveConanRenamer.Models
{
    public class Episode
    {
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class EpisodesData
    {
        public List<Episode> Episodes { get; set; } = new List<Episode>();
    }
} 