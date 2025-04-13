using System.Text.RegularExpressions;

namespace DetectiveConanRenamer.Models
{
    public class RegexPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int CaptureGroup { get; set; } = 1;

        public Regex ToRegex()
        {
            return new Regex(Pattern, RegexOptions.IgnoreCase);
        }
    }
} 