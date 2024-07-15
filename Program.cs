using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace DetectiveConanRenamer
{
    internal class Program
    {
        static void Main()
        {
            while (true)
            {
                Console.Clear();
                AnsiConsole.Write(
                    new FigletText("Detective Conan Episode Renamer")
                        .Centered()
                        .Color(Color.Aqua));

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What do you want to do?")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Rename Episodes",
                            "Dev Menu",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "Rename Episodes":
                        RenameEpisodes(false);
                        break;
                    case "Dev Menu":
                        DevMenu();
                        break;
                    case "Exit":
                        return;
                }

                AnsiConsole.Markup("[green]Press any key to return to the menu...[/]");
                Console.ReadKey();
            }
        }

        static void ConvertEpisodesToJson()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string wikiContentPath = Path.Combine(exeDirectory, "DetectiveConanTitre.txt");
            string jsonOutputPath = Path.Combine(exeDirectory, "DetectiveConanEpisodes.json");

            AnsiConsole.MarkupLine($"[yellow]Reading lines from:[/] {wikiContentPath}");
            string[] lines = File.ReadAllLines(wikiContentPath);
            var episodes = ExtractEpisodes(lines);

            AnsiConsole.MarkupLine("[yellow]Episodes extracted:[/]");
            foreach (var episode in episodes)
            {
                AnsiConsole.MarkupLine($"[blue]Episode {episode.Key}:[/] {episode.Value}");
            }

            if (episodes.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No episodes were extracted. Please check the input file and regex patterns.[/]");
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(episodes, options);
            File.WriteAllText(jsonOutputPath, json);

            AnsiConsole.MarkupLine($"[green]Episodes have been converted to JSON and saved to:[/] {jsonOutputPath}");
        }

        static void RenameEpisodes(bool fromDevMenu)
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string jsonInputPath = Path.Combine(exeDirectory, "DetectiveConanEpisodes.json");

            AnsiConsole.MarkupLine($"[yellow]Reading episodes from:[/] {jsonInputPath}");
            string jsonContent = File.ReadAllText(jsonInputPath);
            var episodes = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);

            if (episodes == null || episodes.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No episodes were found in the JSON file. Please check the JSON content.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[yellow]Episodes extracted from JSON:[/]");
            foreach (var episode in episodes)
            {
                AnsiConsole.MarkupLine($"[blue]Episode {episode.Key}:[/] {episode.Value}");
            }

            string directoryPath;
            if (fromDevMenu)
            {
                directoryPath = Path.Combine(exeDirectory, "TestDirectory");
            }
            else
            {
                directoryPath = AnsiConsole.Ask<string>("Please enter the path to the directory containing the episodes:");
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

                    AnsiConsole.MarkupLine($"[blue]Extracted Episode {episodeNumber}:[/] {episodeTitle}");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Failed to match episode number or title.[/]");
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
            // Remove <ref>...</ref> tags
            title = Regex.Replace(title, @"<ref.*?>.*?</ref>", "", RegexOptions.Singleline).Trim();
            // Replace "{{1re|partie}}" with "1re Partie" and similar patterns
            title = Regex.Replace(title, @"\{\{(\d+(?:re|e))\|partie\}\}", "$1 Partie").Trim();
            // Ensure "1re Partie" comes after the number
            title = Regex.Replace(title, @"(\d+(?:re|e))\s*", "$1 Partie ").Trim();
            // Remove any remaining '{{' and '}}'
            title = Regex.Replace(title, @"\{\{|\}\}", "").Trim();
            // Replace ":" and "–" with "-"
            title = title.Replace(":", "-").Replace("–", "-").Replace(";", "-").Trim();
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
            AnsiConsole.MarkupLine($"[yellow]Renaming files in directory:[/] {directoryPath}");
            string[] extensions = { "*.mkv", "*.mp4", "*.avi" };

            foreach (var extension in extensions)
            {
                string[] files = Directory.GetFiles(directoryPath, extension);

                if (files.Length > 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]Processing files with extension:[/] {extension}");

                    var table = new Table().Expand().Border(TableBorder.Rounded);
                    table.AddColumn("Original Filename");
                    table.AddColumn("New Filename");

                    AnsiConsole.Live(table).Start(ctx =>
                    {
                        foreach (var file in files)
                        {
                            var match = Regex.Match(Path.GetFileName(file), @"\b[Dd](é|e)tective [Cc]onan [Ee]pisode (\d{1,4})\b", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                if (int.TryParse(match.Groups[2].Value, out int episodeNumber))
                                {
                                    if (episodes.ContainsKey(episodeNumber))
                                    {
                                        string newFileName = $"Détective Conan Épisode {episodeNumber:0000} - {episodes[episodeNumber]}{Path.GetExtension(file)}";
                                        string newFilePath = Path.Combine(directoryPath, newFileName);

                                        if (newFilePath.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                                        {
                                            if (File.Exists(newFilePath))
                                            {
                                                File.Delete(newFilePath);
                                            }

                                            File.Move(file, newFilePath);

                                            // Update the live table display
                                            table.AddRow(file, newFileName);
                                            ctx.Refresh();
                                        }
                                        else
                                        {
                                            AnsiConsole.MarkupLine($"[red]Invalid path:[/] {newFilePath}");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine($"[red]No episode title found for episode number:[/] {episodeNumber}");
                                    }
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[red]Failed to parse episode number from:[/] {match.Groups[2].Value}");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]No match for file:[/] {file}");
                            }
                        }
                    });
                }
            }
        }

        static void DevMenu()
        {
            while (true)
            {
                Console.Clear();
                AnsiConsole.Write(
                    new FigletText("Detective Conan Episode Renamer")
                        .Centered()
                        .Color(Color.Aqua));
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Development Menu")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Create Test Directory and Files",
                            "Rename Test Files",
                            "Delete Test Directory",
                            "Open Test Directory",
                            "Convert Episodes to JSON",
                            "Back to Main Menu"
                        }));

                switch (choice)
                {
                    case "Create Test Directory and Files":
                        CreateTestDirectoryAndFiles();
                        break;
                    case "Rename Test Files":
                        RenameTestFiles();
                        break;
                    case "Delete Test Directory":
                        DeleteTestDirectory();
                        break;
                    case "Open Test Directory":
                        OpenTestDirectory();
                        break;
                    case "Convert Episodes to JSON":
                        ConvertEpisodesToJson();
                        break;
                    case "Back to Main Menu":
                        return;
                }

                AnsiConsole.Markup("[green]Press any key to return to the Dev Menu...[/]");
                Console.ReadKey();
            }
        }

        static void CreateTestDirectoryAndFiles()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string testDirectoryPath = Path.Combine(exeDirectory, "TestDirectory");
            Directory.CreateDirectory(testDirectoryPath);

            for (int i = 1; i <= 15; i++)
            {
                string filePath = Path.Combine(testDirectoryPath, $"Detective Conan Episode {i:000}.mkv");
                File.Create(filePath).Dispose();
            }

            AnsiConsole.MarkupLine("[green]Test directory and files created successfully.[/]");
        }

        static void RenameTestFiles()
        {
            RenameEpisodes(true);
        }

        static void DeleteTestDirectory()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string testDirectoryPath = Path.Combine(exeDirectory, "TestDirectory");

            if (Directory.Exists(testDirectoryPath))
            {
                Directory.Delete(testDirectoryPath, true);
                AnsiConsole.MarkupLine("[green]Test directory deleted successfully.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Test directory does not exist.[/]");
            }
        }

        static void OpenTestDirectory()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string testDirectoryPath = Path.Combine(exeDirectory, "TestDirectory");

            if (!Directory.Exists(testDirectoryPath))
            {
                Directory.CreateDirectory(testDirectoryPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = testDirectoryPath,
                UseShellExecute = true,
                Verb = "open"
            });

            AnsiConsole.MarkupLine("[green]Test directory opened successfully.[/]");
        }
    }
}
