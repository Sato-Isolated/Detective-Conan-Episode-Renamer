using Microsoft.Extensions.Options;
using DetectiveConanRenamer.Models;
using System.Text.RegularExpressions;
using DetectiveConanRenamer.Interfaces;
using DetectiveConanRenamer.Utils;

namespace DetectiveConanRenamer.Services
{
    public class FileRenamerService : IFileRenamer
    {
        private readonly ILoggingService _loggingService;
        private readonly IValidationService _validationService;
        private readonly IBackupService _backupService;
        private readonly IRegexPatternService _regexPatternService;

        public FileRenamerService(
            ILoggingService loggingService,
            IValidationService validationService,
            IBackupService backupService,
            IRegexPatternService regexPatternService)
        {
            _loggingService = loggingService;
            _validationService = validationService;
            _backupService = backupService;
            _regexPatternService = regexPatternService;
        }

        public void RenameFiles(string directoryPath, Dictionary<int, string> episodes)
        {
            if (episodes == null || episodes.Count == 0)
            {
                _loggingService.Error("Aucun titre d'épisode disponible. Veuillez d'abord scraper les titres.");
                return;
            }

            var files = Directory.GetFiles(directoryPath)
                .Where(f => IsValidVideoFile(f))
                .ToList();

            if (files.Count == 0)
            {
                _loggingService.Error($"Aucun fichier vidéo trouvé dans le dossier {directoryPath}");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    var episodeNumber = _regexPatternService.ExtractEpisodeNumberAsync(fileName).GetAwaiter().GetResult();

                    if (!episodeNumber.HasValue)
                    {
                        _loggingService.Warning($"Impossible de déterminer le numéro d'épisode pour {fileName}");
                        continue;
                    }

                    if (!episodes.TryGetValue(episodeNumber.Value, out string? episodeTitle))
                    {
                        _loggingService.Warning($"Titre non trouvé pour l'épisode {episodeNumber}");
                        continue;
                    }

                    var newFileName = GenerateNewFileName(fileName, episodeNumber.Value, episodeTitle);
                    var newFilePath = Path.Combine(directoryPath, newFileName);

                    if (File.Exists(newFilePath))
                    {
                        _loggingService.Warning($"Le fichier {newFileName} existe déjà. Renommage ignoré.");
                        continue;
                    }

                    if (AppSettings.CreateBackups)
                    {
                        _backupService.CreateBackupAsync(file).GetAwaiter().GetResult();
                    }

                    File.Move(file, newFilePath);
                    _loggingService.Information($"Fichier renommé : {fileName} -> {newFileName}");
                }
                catch (Exception ex)
                {
                    _loggingService.Error($"Erreur lors du renommage de {file} : {ex.Message}");
                }
            }
        }

        public bool ValidateFilePath(string filePath)
        {
            return _validationService.ValidateFileName(filePath, out _);
        }

        private bool IsValidVideoFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension is ".mp4" or ".mkv";
        }

        public string GenerateNewFileName(string originalFileName, int episodeNumber, string episodeTitle)
        {
            var extension = Path.GetExtension(originalFileName);
            return $"Détective Conan {episodeNumber:D3} - {episodeTitle}{extension}";
        }
    }
} 