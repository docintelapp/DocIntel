using System.IO;

namespace DocIntel.Core.Collectors;

public interface IFileContent
{
    void WriteToFile(string filename);
    Stream Stream();
}