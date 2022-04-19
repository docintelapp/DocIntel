using System.Net;
using DocIntel.Core.Utils.Observables.PostProcessors;
using Synsharp.Forms;

namespace DocIntel.Tests;

public class TestIPAnnotation
{
    [Test]
    public async Task TestAnnotatePrivateIP()
    {
        var p = new PrivateIpPostProcessor();
        var a = InetIPv4.Parse("10.5.7.1");
        var b = InetIPv4.Parse("172.16.12.1");
        var c = InetIPv4.Parse("192.168.1.5");
        var d = InetIPv4.Parse("8.8.8.8");
        await p.Process(new[] { a, b, c, d });
        
        Assert.That(a.Tags, Does.Contain("net.priv"));
        Assert.That(b.Tags, Does.Contain("net.priv"));
        Assert.That(c.Tags, Does.Contain("net.priv"));
        Assert.That(d.Tags, Does.Not.Contain("net.priv"));
    }
}