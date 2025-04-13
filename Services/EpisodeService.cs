using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DetectiveConanRenamer.Interfaces;
using DetectiveConanRenamer.Models;
using DetectiveConanRenamer.Utils;
using Spectre.Console;

namespace DetectiveConanRenamer.Services
{
    public class EpisodeService : IEpisodeService
    {
        private readonly ILoggingService _loggingService;
        private readonly string _episodesFilePath;
        private Dictionary<int, string> _episodes;

        public EpisodeService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _episodesFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                AppSettings.DataDirectory,
                AppSettings.EpisodesFileName);
            _episodes = new Dictionary<int, string>();

            var dataDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                AppSettings.DataDirectory);
            FileUtils.EnsureDirectoryExists(dataDirectory, _loggingService);
            LoadEpisodes();
        }

        private void LoadEpisodes()
        {
            _episodes = FileUtils.LoadEpisodesFromYamlAsync(_episodesFilePath, _loggingService).GetAwaiter().GetResult();
        }

        public Dictionary<int, string> LoadEpisodesFromJson(string jsonPath)
        {
            _episodes = FileUtils.LoadEpisodesFromYamlAsync(jsonPath, _loggingService).GetAwaiter().GetResult();
            return _episodes;
        }

        public async Task<Dictionary<int, string>> GetEpisodesAsync()
        {
            return await Task.FromResult(_episodes);
        }

        public async Task SaveEpisodesAsync(Dictionary<int, string> episodes)
        {
            _episodes = episodes;
            await FileUtils.SaveEpisodesToYamlAsync(_episodesFilePath, _episodes, _loggingService);
        }

        public async Task ReloadEpisodesAsync()
        {
            _episodes = await FileUtils.LoadEpisodesFromYamlAsync(_episodesFilePath, _loggingService);
            _loggingService.Information($"Épisodes rechargés depuis {_episodesFilePath}");
        }
    }
} 