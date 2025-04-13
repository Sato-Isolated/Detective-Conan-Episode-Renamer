using System.Collections.Generic;

namespace DetectiveConanRenamer.Interfaces
{
    public interface IFileRenamer
    {
        void RenameFiles(string directoryPath, Dictionary<int, string> episodes);
        bool ValidateFilePath(string filePath);
        string GenerateNewFileName(string originalFileName, int episodeNumber, string episodeTitle);
    }
} 