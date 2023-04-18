using System.IO;

namespace DocIntel.Core.Collectors;

public class StreamContent : IFileContent
{
    private Stream? _stream;

    public StreamContent(Stream? stream)
    {
        _stream = stream;
    }

    public void WriteToFile(string filename)
    {
        if (_stream != null)
        {
            var fileStream = File.Create(filename);
            if (_stream.CanSeek)
                _stream.Seek(0, SeekOrigin.Begin);
            _stream.CopyTo(fileStream);
            fileStream.Close();
        }
    }

    public Stream Stream() => _stream;
}