using DetectiveConanRenamer.Models;
using DetectiveConanRenamer.Interfaces;

namespace DetectiveConanRenamer.Services
{
    public interface IBackupService
    {
        Task CreateBackupAsync(string filePath);
    }

    public class BackupService : IBackupService
    {
        private readonly ILoggingService _loggingService;
        private readonly string _backupDirectory;

        public BackupService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _backupDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                AppSettings.BackupDirectory);

            EnsureBackupDirectoryExists();
        }

        private void EnsureBackupDirectoryExists()
        {
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                _loggingService.Information($"Répertoire de sauvegarde créé : {_backupDirectory}");
            }
        }

        public async Task CreateBackupAsync(string filePath)
        {
            if (!AppSettings.CreateBackups)
            {
                _loggingService.Information("Les sauvegardes sont désactivées.");
                return;
            }

            try
            {
                var fileName = Path.GetFileName(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
                var backupPath = Path.Combine(_backupDirectory, backupFileName);

                await Task.Run(() =>
                {
                    File.Copy(filePath, backupPath, true);
                    _loggingService.Information($"Sauvegarde créée : {backupFileName}");
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Erreur lors de la création de la sauvegarde : {ex.Message}");
                throw;
            }
        }
    }
} 