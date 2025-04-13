using System;
using System.IO;
using System.Threading.Tasks;
using DetectiveConanRenamer.Interfaces;
using DetectiveConanRenamer.Models;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DetectiveConanRenamer.Utils
{
    public static class FileUtils
    {
        private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static void EnsureDirectoryExists(string directoryPath, ILoggingService loggingService)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                loggingService.Information($"Répertoire créé : {directoryPath}");
            }
        }

        public static async Task<Dictionary<int, string>> LoadEpisodesFromYamlAsync(string filePath, ILoggingService loggingService)
        {
            if (!File.Exists(filePath))
            {
                loggingService.Warning($"Fichier des épisodes non trouvé : {filePath}");
                return new Dictionary<int, string>();
            }

            try
            {
                var yaml = await File.ReadAllTextAsync(filePath);
                var episodesData = _yamlDeserializer.Deserialize<EpisodesData>(yaml) ?? new EpisodesData();
                var episodes = episodesData.Episodes.ToDictionary(e => e.Number, e => e.Title);
                loggingService.Information($"{episodes.Count} épisodes chargés depuis {filePath}");
                return episodes;
            }
            catch (Exception ex)
            {
                loggingService.Error($"Erreur lors du chargement des épisodes : {ex.Message}");
                return new Dictionary<int, string>();
            }
        }

        public static async Task SaveEpisodesToYamlAsync(string filePath, Dictionary<int, string> episodes, ILoggingService loggingService)
        {
            try
            {
                var episodesData = new EpisodesData
                {
                    Episodes = episodes.Select(kvp => new Episode { Number = kvp.Key, Title = kvp.Value }).ToList()
                };
                var yaml = _yamlSerializer.Serialize(episodesData);
                await File.WriteAllTextAsync(filePath, yaml, Encoding.UTF8);
                loggingService.Information($"Épisodes sauvegardés dans {filePath}");
            }
            catch (Exception ex)
            {
                loggingService.Error($"Erreur lors de la sauvegarde des épisodes : {ex.Message}");
                throw;
            }
        }

        public static int ExtractEpisodeNumber(string fileName)
        {
            // Pattern pour les épisodes normaux
            var match = Regex.Match(fileName, @"D[ée]tective\s+Conan\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int episodeNumber))
            {
                return episodeNumber;
            }

            // Pattern pour les épisodes spéciaux (ex: "11 - SP1")
            match = Regex.Match(fileName, @"D[ée]tective\s+Conan\s+(\d+)\s*-\s*SP(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int baseNumber) && int.TryParse(match.Groups[2].Value, out int spNumber))
            {
                // On retourne le numéro de base pour les épisodes spéciaux
                return baseNumber;
            }

            throw new ArgumentException($"Impossible d'extraire le numéro d'épisode du fichier : {fileName}");
        }

        public static string CleanFileName(string fileName)
        {
            // Remplacer les caractères invalides par des espaces
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanName = invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, ' '));

            // Remplacer les espaces multiples par un seul espace
            cleanName = Regex.Replace(cleanName, @"\s+", " ");

            // Supprimer les espaces au début et à la fin
            cleanName = cleanName.Trim();

            // Limiter la longueur du nom de fichier
            const int maxLength = 100;
            if (cleanName.Length > maxLength)
            {
                cleanName = cleanName[..maxLength];
            }

            return cleanName;
        }
    }
} 