using System;
using System.IO;

namespace DocIntel.Core.Collectors;

public class WebContent : IFileContent
{
    private Uri _uri;

    public WebContent(Uri uri)
    {
        _uri = uri;
    }

    public void WriteToFile(string filename)
    {
        throw new NotImplementedException();
    }

    public Stream Stream()
    {
        throw new NotImplementedException();
    }
}