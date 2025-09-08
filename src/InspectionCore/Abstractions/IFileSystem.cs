using System.IO;

namespace InspectionCore.Abstractions
{
    public interface IFileSystem
    {
        void CreateDirectory(string path);
        Stream OpenWrite(string path);
        bool FileExists(string path);
    }

    public sealed class RealFileSystem : IFileSystem
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public Stream OpenWrite(string path) => File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        public bool FileExists(string path) => File.Exists(path);
    }
}
