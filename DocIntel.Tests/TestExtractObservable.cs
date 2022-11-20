using DocIntel.Core.Utils.Observables;
using DocIntel.Core.Utils.Observables.Extractors;
using Microsoft.Extensions.Logging;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.Tests;

public class TestExtractObservable
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestIpNotExtractedAsUrl()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var logger = loggerFactory.CreateLogger<RegexUrlExtractor>();

        var test = @"mb.glbaitech[.]com – MoonBounce
        ns.glbaitech[.]com – ScrambleCross
        dev.kinopoisksu[.]com – ScrambleCross
        st.kinopoisksu[.]com – ScrambleCross
        188.166.61[.]146 – ScrambleCross
        172.107.231[.]236 – ScrambleCross
        193.29.57[.]161 – ScrambleCross
        136.244.100[.]127 – ScrambleCross
        217.69.10[.]104 – ScrambleCross
        92.38.178[.]246 – ScrambleCross
        m.necemarket[.]com – Microcin
        172.105.94[.]67 – Microcin
        holdmem.dbhubspi[.]com – Microcin
        5.188.93[.]132 – Go malware
        5.189.222[.]33 – Go malware
        5.183.103[.]122 – Go malware
        5.188.108[.]228 – Go malware
        45.128.132[.]6 – Go malware
        92.223.105[.]246 – Go malware
        5.183.101[.]21 – Go malware
        5.183.101[.]114 – Go malware
        45.128.135[.]15 – Go malware
        5.188.108[.]22 – Go malware
        70.34.201[.]16 – Go malware ";
        
        var extractor = new RegexUrlExtractor(logger);
        var observables = await extractor.Extract(test).ToListAsync();
        
        Assert.That(observables.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task TestIpExtracted()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var logger = loggerFactory.CreateLogger<RegexIpExtractor>();
        
        var test = @"mb.glbaitech[.]com – MoonBounce
        ns.glbaitech[.]com – ScrambleCross
        dev.kinopoisksu[.]com – ScrambleCross
        st.kinopoisksu[.]com – ScrambleCross
        188.166.61[.]146 – ScrambleCross
        172.107.231[.]236 – ScrambleCross
        193.29.57[.]161 – ScrambleCross
        136.244.100[.]127 – ScrambleCross
        217.69.10[.]104 – ScrambleCross
        92.38.178[.]246 – ScrambleCross
        m.necemarket[.]com – Microcin
        172.105.94[.]67 – Microcin
        holdmem.dbhubspi[.]com – Microcin
        5.188.93[.]132 – Go malware
        5.189.222[.]33 – Go malware
        5.183.103[.]122 – Go malware
        5.188.108[.]228 – Go malware
        45.128.132[.]6 – Go malware
        92.223.105[.]246 – Go malware
        5.183.101[.]21 – Go malware
        5.183.101[.]114 – Go malware
        45.128.135[.]15 – Go malware
        5.188.108[.]22 – Go malware
        70.34.201[.]16 – Go malware ";
        var extractor = new RegexIpExtractor();
        var observables = await extractor.Extract(test).ToListAsync();
        
        Assert.That(observables.Count, Is.EqualTo(18));
        
        Assert.That(observables, Contains.Item(InetIPv4.Parse("188.166.61.146"))); 
        Assert.That(observables, Contains.Item(InetIPv4.Parse("172.107.231.236")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("193.29.57.161")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("136.244.100.127")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("217.69.10.104")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("92.38.178.246")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("172.105.94.67")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.188.93.132"))); 
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.189.222.33")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.183.103.122")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.188.108.228")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("45.128.132.6"))); 
        Assert.That(observables, Contains.Item(InetIPv4.Parse("92.223.105.246"))); 
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.183.101.21"))); 
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.183.101.114")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("45.128.135.15")));
        Assert.That(observables, Contains.Item(InetIPv4.Parse("5.188.108.22"))); 
        Assert.That(observables, Contains.Item(InetIPv4.Parse("70.34.201.16")));
    }
    
    [Test]
    public async Task TestUrlExtracted()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var logger = loggerFactory.CreateLogger<RegexUrlExtractor>();
        
        var test = @"This multistage chain of hooks facilitates the propagation of malicious code from the CORE_DXE
        image to other boot components during system startup, allowing the introduction of a malicious
        driver to the memory address space of the Windows kernel. This driver, which runs during the
        initial phases of the kernel’s execution, is in charge of deploying user-mode malware by injecting it
        into an svchost.exe process, once the operating system is up and running. Finally, the user mode
        malware reaches out to a hardcoded C&C URL (i.e. hxxp://mb.glbaitech[.]com/mboard.dll) and
        attempts to fetch another stage of the payload to run in memory, which we were not able to
        retrieve. ";
        
        var extractor = new RegexUrlExtractor(logger);
        var observables = await extractor.Extract(test).ToListAsync();
        
        Assert.That(observables.Count, Is.EqualTo(1));

        Assert.That(observables, Contains.Item(InetUrl.Parse("http://mb.glbaitech.com/mboard.dll")));
    }
    
    [Test]
    public async Task TestUrlExtractedWithBackslashes()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var logger = loggerFactory.CreateLogger<RegexUrlExtractor>();
        
        var test = @"This multistage chain of hooks facilitates the propagation of malicious code from the CORE_DXE
        image to other boot components during system startup, allowing the introduction of a malicious
        driver to the memory address space of the Windows kernel. This driver, which runs during the
        initial phases of the kernel’s execution, is in charge of deploying user-mode malware by injecting it
        into an svchost.exe process, once the operating system is up and running. Finally, the user mode
        malware reaches out to a hardcoded C&C URL (i.e. hxxp://mb\.glbaitech\.com/mboard.dll) and
        attempts to fetch another stage of the payload to run in memory, which we were not able to
        retrieve. ";
        
        var extractor = new RegexUrlExtractor(logger);
        var observables = await extractor.Extract(test).ToListAsync();
        
        Assert.That(observables.Count, Is.EqualTo(1));

        Assert.That(observables, Contains.Item(InetUrl.Parse("http://mb.glbaitech.com/mboard.dll")));
    }
    
    [Test]
    public async Task TestUrlExtractedWithSquaredBrackets()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var logger = loggerFactory.CreateLogger<RegexUrlExtractor>();
        
        var test = @"HolyRS.exe/BTLC.exe C2 URL pattern:

hxxp://193[.]56[.]29[.]123:8888/access.php?order=GetPubkey&cmn=[Victim_HostName]
hxxp://193[.]56[.]29[.]123:8888/access.php?order=golc_key_add&cmn=[Victim_HostName]&type=1
hxxp://193[.]56[.]29[.]123:8888/access.php?order=golc_key_add&cmn=[Victim_HostName]&type=2
hxxp://193[.]56[.]29[.]123:8888/access.php?order=golc_finish&cmn=[Victim_HostName]& ";
        
        var extractor = new RegexUrlExtractor(logger);
        var observables = await extractor.Extract(test).ToListAsync();
        
        Assert.That(observables.Count, Is.EqualTo(4));

        // We have to settle here for less ideal extraction with the last ]
        Assert.That(observables, Contains.Item(InetUrl.Parse("http://193.56.29.123:8888/access.php?order=GetPubkey&cmn=[Victim_HostName")));
        Assert.That(observables, Contains.Item(InetUrl.Parse("http://193.56.29.123:8888/access.php?order=golc_key_add&cmn=[Victim_HostName]&type=1")));
        Assert.That(observables, Contains.Item(InetUrl.Parse("http://193.56.29.123:8888/access.php?order=golc_key_add&cmn=[Victim_HostName]&type=2")));
        Assert.That(observables, Contains.Item(InetUrl.Parse("http://193.56.29.123:8888/access.php?order=golc_finish&cmn=[Victim_HostName]&")));
    }
    
    [Test]
    public async Task TestUrlExtractedWithParenthesis()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var logger = loggerFactory.CreateLogger<RegexUrlExtractor>();
        
        var test = @"hxxp://193[.]56[.]29[.]123:8888/access.php)";
        
        var extractor = new RegexUrlExtractor(logger);
        var observables = await extractor.Extract(test).ToListAsync();
        
        Assert.That(observables.Count, Is.EqualTo(1));

        Assert.That(observables, Contains.Item(InetUrl.Parse("http://193.56.29.123:8888/access.php")));
        Assert.That(observables, Does.Not.Contains(InetUrl.Parse("http://193.56.29.123:8888/access.php)")));
    }
    
}