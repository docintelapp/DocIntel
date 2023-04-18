using System.IO;

namespace DocIntel.Core.Collectors;

public class StringContent : IFileContent
{
    private string _string;

    public StringContent(string s)
    {
        _string = s;
    }

    public void WriteToFile(string filename)
    {
        File.WriteAllText(filename, _string);
    }

    public Stream Stream()
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(_string);
        MemoryStream stream = new MemoryStream(buffer);
        return stream;
    }
}