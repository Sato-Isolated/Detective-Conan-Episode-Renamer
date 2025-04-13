using System;
using System.IO;

namespace DetectiveConanRenamer.Models
{
    public class AppConfiguration
    {
        public string DataDirectory { get; set; }
        public string EpisodesFileName { get; set; }
        public string BackupDirectory { get; set; }
        public bool CreateBackups { get; set; }

        public AppConfiguration()
        {
            DataDirectory = AppSettings.GetDataDirectory();
            EpisodesFileName = AppSettings.EpisodesFileName;
            BackupDirectory = AppSettings.BackupDirectory;
            CreateBackups = AppSettings.CreateBackups;
        }
    }

    public static class AppSettings
    {
        private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string DataDirectory { get; private set; } = Path.Combine(AppDirectory, "data");

        public static string EpisodesFileName { get; private set; } = "episodes.yaml";
        public static string BackupDirectory { get; private set; } = Path.Combine(AppDirectory, "backups");
        public static bool CreateBackups { get; } = true;
        public static string RegexPatternsFileName { get; } = "regex-patterns.yaml";
        public static List<RegexPattern> DefaultRegexPatterns { get; } = new()
        {
            new RegexPattern
            {
                Name = "Détective Conan Standard",
                Pattern = @"Détective\s+Conan\s+(\d+)",
                Description = "Format standard: Détective Conan 123",
                IsDefault = true,
                CaptureGroup = 1
            },
            new RegexPattern
            {
                Name = "Detective Conan Standard",
                Pattern = @"Detective\s+Conan\s+(\d+)",
                Description = "Format standard sans accents: Detective Conan 123",
                IsDefault = true,
                CaptureGroup = 1
            },
            new RegexPattern
            {
                Name = "Format Simple",
                Pattern = @"(\d+)",
                Description = "Format simple: juste le numéro d'épisode",
                IsDefault = true,
                CaptureGroup = 1
            }
        };

        static AppSettings()
        {
            // Créer le dossier data s'il n'existe pas
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }

        public static string GetDataDirectory()
        {
            return DataDirectory;
        }

        public static void SetEpisodesFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Le nom du fichier ne peut pas être vide.");
            }
            EpisodesFileName = fileName;
        }

        public static void SetBackupDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Le dossier {path} n'existe pas.");
            }
            BackupDirectory = path;
        }

        public static void SetDataDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Le dossier {path} n'existe pas.");
            }
            DataDirectory = path;
        }

        public static string GetEpisodesFilePath()
        {
            return Path.Combine(DataDirectory, EpisodesFileName);
        }

        public static string GetRegexPatternsFilePath()
        {
            return Path.Combine(DataDirectory, RegexPatternsFileName);
        }
    }
} 