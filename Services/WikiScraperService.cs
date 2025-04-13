using System.Web;
using HtmlAgilityPack;
using DetectiveConanRenamer.Models;
using DetectiveConanRenamer.Utils;
using DetectiveConanRenamer.Interfaces;
using System.Text.RegularExpressions;

namespace DetectiveConanRenamer.Services
{
    public interface IWikiScraperService
    {
        Task ScrapeAllSeasonsAsync();
        Task ScrapeSeasonAsync(int seasonNumber);
        Task UpdateEpisodes(Dictionary<int, string> episodes);
    }

    public class WikiScraperService : IWikiScraperService
    {
        private readonly ILoggingService _loggingService;
        private readonly HttpClient _httpClient;
        private readonly string _episodesFilePath;
        private readonly Dictionary<int, string> _episodes;

        public WikiScraperService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _httpClient = new HttpClient();
            
            // Création du chemin complet vers le fichier des épisodes
            var dataDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                AppSettings.DataDirectory);
            
            // S'assurer que le dossier Data existe
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
                _loggingService.Information($"Dossier Data créé à {dataDirectory}");
            }
            
            _episodesFilePath = Path.Combine(dataDirectory, AppSettings.EpisodesFileName);
            _episodes = new Dictionary<int, string>();

            // Charger les épisodes existants ou créer un nouveau fichier
            LoadExistingEpisodes();
        }

        private void LoadExistingEpisodes()
        {
            try
            {
                if (File.Exists(_episodesFilePath))
                {
                    var loadedEpisodes = FileUtils.LoadEpisodesFromYamlAsync(_episodesFilePath, _loggingService).GetAwaiter().GetResult();
                    foreach (var episode in loadedEpisodes)
                    {
                        _episodes[episode.Key] = episode.Value;
                    }
                    _loggingService.Information($"Chargement de {_episodes.Count} épisodes depuis {_episodesFilePath}");
                }
                else
                {
                    _loggingService.Information($"Création d'un nouveau fichier d'épisodes à {_episodesFilePath}");
                    SaveEpisodesAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Erreur lors du chargement des épisodes : {ex.Message}");
            }
        }

        private async Task SaveEpisodesAsync()
        {
            try
            {
                await FileUtils.SaveEpisodesToYamlAsync(_episodesFilePath, _episodes, _loggingService);
                _loggingService.Information($"Sauvegarde de {_episodes.Count} épisodes dans {_episodesFilePath}");
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Erreur lors de la sauvegarde des épisodes : {ex.Message}");
            }
        }

        public async Task ScrapeAllSeasonsAsync()
        {
            _loggingService.Information("Début du scraping de toutes les saisons");
            const int MAX_SEASONS = 30;

            for (int seasonNumber = 1; seasonNumber <= MAX_SEASONS; seasonNumber++)
            {
                try
                {
                    await ScrapeSeasonAsync(seasonNumber);
                }
                catch (Exception ex)
                {
                    _loggingService.Error($"Erreur lors du scraping de la saison {seasonNumber} : {ex.Message}");
                    break;
                }
            }

            await SaveEpisodesAsync();
            _loggingService.Information($"Scraping terminé. {_episodes.Count} épisodes trouvés.");
        }

        public async Task ScrapeSeasonAsync(int seasonNumber)
        {
            try
            {
                var url = $"https://fr.wikipedia.org/wiki/Saison_{seasonNumber}_de_Détective_Conan";
                _loggingService.Information($"Scraping de la saison {seasonNumber} depuis {url}");

                var html = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'wikitable')]");
                if (table == null)
                {
                    _loggingService.Error($"Table des épisodes non trouvée pour la saison {seasonNumber}");
                    return;
                }

                var rows = table.SelectNodes(".//tr");
                if (rows == null)
                {
                    _loggingService.Error($"Aucun épisode trouvé pour la saison {seasonNumber}");
                    return;
                }

                foreach (var row in rows)
                {
                    var cells = row.SelectNodes(".//th|.//td");
                    if (cells == null || cells.Count < 2) continue;

                    var episodeNumberText = cells[0].InnerText.Trim();
                    if (!int.TryParse(episodeNumberText, out int episodeNumber))
                    {
                        // Vérifie si c'est un épisode spécial (ex: "11 - SP1")
                        var spMatch = Regex.Match(episodeNumberText, @"(\d+)\s*-\s*SP(\d+)");
                        if (spMatch.Success && int.TryParse(spMatch.Groups[1].Value, out int baseNumber))
                        {
                            episodeNumber = baseNumber;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var title = cells[1].InnerText.Trim();
                    title = HttpUtility.HtmlDecode(title);

                    if (!string.IsNullOrEmpty(title))
                    {
                        // Nettoie les caractères interdits par Windows
                        var invalidChars = Path.GetInvalidFileNameChars();
                        foreach (var c in invalidChars)
                        {
                            title = title.Replace(c, '-');
                        }

                        // Remplace tous les espaces (normaux et insécables) par des espaces normaux
                        title = Regex.Replace(title, @"[\s\u00A0]+", " ");

                        // Formate la partie (files - X tome - Y) de manière cohérente
                        title = Regex.Replace(title, @"\(files\s*[-:]?\s*(\d+(?:-\d+)?)\s*[-:]?\s*tomes?\s*[-:]?\s*(\d+(?:-\d+)?)\)", match =>
                        {
                            var files = match.Groups[1].Value;
                            var tomes = match.Groups[2].Value;
                            return $"(files - {files} tomes - {tomes})";
                        });

                        _episodes[episodeNumber] = title;
                        _loggingService.Information($"Épisode {episodeNumber}: {title}");
                    }
                }

                await SaveEpisodesAsync();
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Erreur lors du scraping de la saison {seasonNumber}", ex);
            }
        }

        public async Task UpdateEpisodes(Dictionary<int, string> episodes)
        {
            foreach (var episode in episodes)
            {
                _episodes[episode.Key] = episode.Value;
            }
            await SaveEpisodesAsync();
            _loggingService.Information($"{episodes.Count} épisodes mis à jour.");
        }
    }
} 