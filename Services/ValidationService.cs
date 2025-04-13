using FluentValidation;
using DetectiveConanRenamer.Models;

namespace DetectiveConanRenamer.Services
{
    public interface IValidationService
    {
        bool ValidateDirectory(string path, out string errorMessage);
        bool ValidateSeasonNumber(int seasonNumber, out string errorMessage);
        bool ValidateFileName(string fileName, out string errorMessage);
    }

    public class ValidationService : IValidationService
    {
        private readonly IValidator<AppConfiguration> _settingsValidator;

        public ValidationService()
        {
            _settingsValidator = new AppConfigurationValidator();
        }

        public bool ValidateDirectory(string path, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "Le chemin du dossier ne peut pas être vide.";
                return false;
            }

            if (!Directory.Exists(path))
            {
                errorMessage = "Le dossier spécifié n'existe pas.";
                return false;
            }

            try
            {
                var testFile = Path.Combine(path, "test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Le dossier n'est pas accessible : {ex.Message}";
                return false;
            }
        }

        public bool ValidateSeasonNumber(int seasonNumber, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (seasonNumber <= 0)
            {
                errorMessage = "Le numéro de saison doit être supérieur à 0.";
                return false;
            }

            if (seasonNumber > 1000) // Valeur arbitraire, à ajuster selon les besoins
            {
                errorMessage = "Le numéro de saison semble invalide.";
                return false;
            }

            return true;
        }

        public bool ValidateFileName(string fileName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                errorMessage = "Le nom du fichier ne peut pas être vide.";
                return false;
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
            {
                errorMessage = "Le nom du fichier contient des caractères invalides.";
                return false;
            }

            return true;
        }
    }

    public class AppConfigurationValidator : AbstractValidator<AppConfiguration>
    {
        public AppConfigurationValidator()
        {
            RuleFor(x => x.DataDirectory)
                .NotEmpty()
                .WithMessage("Le répertoire de données est requis.");

            RuleFor(x => x.EpisodesFileName)
                .NotEmpty()
                .WithMessage("Le nom du fichier des épisodes est requis.");

            RuleFor(x => x.BackupDirectory)
                .NotEmpty()
                .WithMessage("Le répertoire de sauvegarde est requis.");
        }
    }
} 