using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        RenameEpisodes();
        Console.ReadKey();
    }

    static void RenameEpisodes()
    {
        string wikiContentPath = @"path\to\DetectiveConanTitre.txt";
        string directoryPath = @"path\to\your\detectiveconanfolder";

        Console.WriteLine($"Reading lines from: {wikiContentPath}");
        string[] lines = File.ReadAllLines(wikiContentPath);
        var episodes = ExtractEpisodes(lines);

        Console.WriteLine("Episodes extracted:");
        foreach (var episode in episodes)
        {
            Console.WriteLine($"Episode {episode.Key}: {episode.Value}");
        }

        if (episodes.Count == 0)
        {
            Console.WriteLine("No episodes were extracted. Please check the input file and regex patterns.");
            return;
        }

        RenameFiles(directoryPath, episodes);
    }

    static Dictionary<int, string> ExtractEpisodes(string[] lines)
    {
        var episodes = new Dictionary<int, string>();
        var episodeBlockRegex = new Regex(@"\{\{Épisode anime\s*\|[\s\S]*?\}\}", RegexOptions.Singleline);
        var numberRegex = new Regex(@"\| NumeroEpisode\s*=\s*(\d+)");
        var titleRegex = new Regex(@"\| TitreFrançais\s*=\s*([^|]+)");

        string content = string.Join("\n", lines);
        var matches = episodeBlockRegex.Matches(content);

        foreach (Match match in matches)
        {
            var episodeBlock = match.Value;
            var numberMatch = numberRegex.Match(episodeBlock);
            var titleMatch = titleRegex.Match(episodeBlock);

            if (numberMatch.Success && titleMatch.Success)
            {
                int episodeNumber = int.Parse(numberMatch.Groups[1].Value);
                string episodeTitle = titleMatch.Groups[1].Value.Trim();
                episodeTitle = CleanEpisodeTitle(episodeTitle);
                episodes[episodeNumber] = episodeTitle;

                Console.WriteLine($"Extracted Episode {episodeNumber}: {episodeTitle}");
            }
            else
            {
                Console.WriteLine("Failed to match episode number or title.");
            }
        }

        return episodes;
    }

    static string CleanEpisodeTitle(string title)
    {
        // Remove "(files: ...)"
        title = Regex.Replace(title, @"\(files[^\)]+\)", "").Trim();
        // Remove content within "<small>...</small>"
        title = Regex.Replace(title, @"<small>.*?</small>", "", RegexOptions.Singleline).Trim();
        // Replace "{{1re|partie}}" with "1re Partie" and similar patterns
        title = Regex.Replace(title, @"\{\{(\d+(?:re|e))\|partie\}\}", "$1 Partie").Trim();
        // Ensure "1re Partie" comes after the number
        title = Regex.Replace(title, @"(\d+(?:re|e))\s*", "$1 Partie ").Trim();
        // Remove any remaining '{{' and '}}'
        title = Regex.Replace(title, @"\{\{|\}\}", "").Trim();
        // Replace ":" and "–" with "-"
        title = title.Replace(":", "-").Replace("–", "-").Trim();
        // Replace quotes with '
        title = title.Replace("\"", "'");
        // Replace invalid characters for file paths
        title = Regex.Replace(title, @"[<>:""/\\|?*]", "-").Trim();
        // Clean up multiple hyphens and spaces
        title = Regex.Replace(title, @"-{2,}", "-");
        title = Regex.Replace(title, @"\s{2,}", " ");
        return title;
    }

    static void RenameFiles(string directoryPath, Dictionary<int, string> episodes)
    {
        Console.WriteLine($"Renaming files in directory: {directoryPath}");
        string[] extensions = { "*.mkv", "*.mp4", "*.avi" };
        foreach (var extension in extensions)
        {
            Console.WriteLine($"Processing files with extension: {extension}");
            string[] files = Directory.GetFiles(directoryPath, extension);
            foreach (var file in files)
            {
                Console.WriteLine($"Checking file: {file}");
                var match = Regex.Match(Path.GetFileName(file), @"\b[Dd](é|e)tective [Cc]onan [Ee]pisode (\d{1,4})\b", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[2].Value, out int episodeNumber))
                    {
                        Console.WriteLine($"Matched episode number: {episodeNumber}");
                        if (episodes.ContainsKey(episodeNumber))
                        {
                            string newFileName = $"Détective Conan Épisode {episodeNumber:0000} - {episodes[episodeNumber]}{Path.GetExtension(file)}";
                            string newFilePath = Path.Combine(directoryPath, newFileName);

                            if (newFilePath.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                            {
                                if (File.Exists(newFilePath))
                                {
                                    Console.WriteLine($"Deleting existing file: {newFilePath}");
                                    File.Delete(newFilePath);
                                }

                                Console.WriteLine($"Renaming: {file} to {newFilePath}");
                                File.Move(file, newFilePath);
                            }
                            else
                            {
                                Console.WriteLine($"Invalid path: {newFilePath}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No episode title found for episode number: {episodeNumber}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse episode number from: {match.Groups[2].Value}");
                    }
                }
                else
                {
                    Console.WriteLine($"No match for file: {file}");
                }
            }
        }
    }
}
