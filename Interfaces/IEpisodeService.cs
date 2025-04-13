using System.Collections.Generic;
using System.Threading.Tasks;
using DetectiveConanRenamer.Models;

namespace DetectiveConanRenamer.Interfaces
{
    public interface IEpisodeService
    {
        Dictionary<int, string> LoadEpisodesFromJson(string jsonPath);
        Task<Dictionary<int, string>> GetEpisodesAsync();
        Task SaveEpisodesAsync(Dictionary<int, string> episodes);
        Task ReloadEpisodesAsync();
    }
} 