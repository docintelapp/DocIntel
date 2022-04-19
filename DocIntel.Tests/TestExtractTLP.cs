using DocIntel.Core.Utils.Features;
using Microsoft.Extensions.Logging;

namespace DocIntel.Tests;

public class TestExtractTLP
{
    [Test]
    public void TestExtractTLPNames()
    {
        var text = @"tlp:green TLP:amber TlP:AmbEr-Strict TLP:RED TLP-white";
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var extractor = new TLPFeatureExtractor();
        var tlp = extractor.Extract(text);

        Console.WriteLine(string.Join(",", tlp));
        
        Assert.That(tlp.Count(), Is.EqualTo(5));
        Assert.That(tlp, Contains.Item("green"));
        Assert.That(tlp, Contains.Item("amber"));
        Assert.That(tlp, Contains.Item("amber-strict"));
        Assert.That(tlp, Contains.Item("red"));
        Assert.That(tlp, Contains.Item("white"));
    }
}