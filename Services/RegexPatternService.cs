using System.Text.Json;
using DetectiveConanRenamer.Models;
using DetectiveConanRenamer.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DetectiveConanRenamer.Services
{
    public interface IRegexPatternService
    {
        Task<List<RegexPattern>> GetPatternsAsync();
        Task SavePatternsAsync(List<RegexPattern> patterns);
        Task<int?> ExtractEpisodeNumberAsync(string fileName);
    }

    public class RegexPatternService : IRegexPatternService
    {
        private readonly ILoggingService _loggingService;
        private readonly string _patternsFilePath;
        private List<RegexPattern> _patterns;
        private readonly ISerializer _yamlSerializer;
        private readonly IDeserializer _yamlDeserializer;

        public RegexPatternService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _patternsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                AppSettings.DataDirectory,
                AppSettings.RegexPatternsFileName);
            _patterns = new List<RegexPattern>();

            _yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            LoadPatterns();
        }

        private void LoadPatterns()
        {
            try
            {
                if (File.Exists(_patternsFilePath))
                {
                    var yaml = File.ReadAllText(_patternsFilePath);
                    _patterns = _yamlDeserializer.Deserialize<List<RegexPattern>>(yaml) ?? new List<RegexPattern>();
                    _loggingService.Information($"Patterns regex chargés depuis {_patternsFilePath}");
                }
                else
                {
                    _patterns = AppSettings.DefaultRegexPatterns;
                    SavePatternsAsync(_patterns).GetAwaiter().GetResult();
                    _loggingService.Information("Patterns regex par défaut chargés");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Erreur lors du chargement des patterns regex : {ex.Message}");
                _patterns = AppSettings.DefaultRegexPatterns;
            }
        }

        public async Task<List<RegexPattern>> GetPatternsAsync()
        {
            if (_patterns.Count == 0)
            {
                await Task.Run(() => LoadPatterns());
            }
            return _patterns;
        }

        public async Task SavePatternsAsync(List<RegexPattern> patterns)
        {
            _patterns = patterns;
            var yaml = _yamlSerializer.Serialize(patterns);
            await File.WriteAllTextAsync(_patternsFilePath, yaml);
            _loggingService.Information($"Patterns regex sauvegardés dans {_patternsFilePath}");
        }

        public async Task<int?> ExtractEpisodeNumberAsync(string fileName)
        {
            foreach (var pattern in _patterns.Where(p => p.IsEnabled))
            {
                var regex = pattern.ToRegex();
                var match = regex.Match(fileName);
                if (match.Success && match.Groups.Count > pattern.CaptureGroup)
                {
                    var group = match.Groups[pattern.CaptureGroup];
                    if (int.TryParse(group.Value, out int episodeNumber))
                    {
                        return episodeNumber;
                    }
                }
            }
            return null;
        }
    }
} 