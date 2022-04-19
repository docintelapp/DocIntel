using DocIntel.Core.Utils.Features;
using Microsoft.Extensions.Logging;

namespace DocIntel.Tests;

public class TestExtractActorName
{
    [Test]
    public void TestExtractMandiantNames()
    {
        var text = @"Since early 2021, Mandiant has been tracking extensive APT29 phishing campaigns targeting 
diplomatic organizations in Europe, the Americas, and Asia. This blog post discusses our recent observations related 
to the identification of two new malware families in 2022, BEATDROP and BOOMMIC, as well as APT29’s efforts to evade 
detection through retooling and abuse of Atlassian's Trello service.
    APT29 is a Russian espionage group that Mandiant has been tracking since at least 2014 and is likely sponsored by 
the Foreign Intelligence Service (SVR). The diplomatic-centric targeting of this recent activity is consistent with 
Russian strategic priorities as well as historic APT29 targeting. Mandiant previously tracked this intrusion activity 
under multiple clusters, UNC2652 and UNC2542, which were recently merged into APT29 in April 2022. Some APT29 activity 
is also publicly referred to as Nobelium by Microsoft.";
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var extractor = new ActorFeatureExtractor();
        var actors = extractor.Extract(text);

        Console.WriteLine(string.Join(",", actors));
        
        Assert.That(actors.Count(), Is.EqualTo(3));
        Assert.That(actors, Contains.Item("APT29"));
        Assert.That(actors, Contains.Item("UNC2652"));
        Assert.That(actors, Contains.Item("UNC2542"));
    }
}