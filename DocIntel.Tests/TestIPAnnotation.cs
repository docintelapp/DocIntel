using DocIntel.Core.Utils.Observables.PostProcessors;
using Synsharp.Telepath.Messages;

namespace DocIntel.Tests;

public class TestIPAnnotation
{
    [Test]
    public async Task TestAnnotatePrivateIP()
    {
        var p = new PrivateIpPostProcessor();
        var a = new SynapseNode() { Form = "inet:ipv4", Valu = "10.5.7.1" };
        var b = new SynapseNode() { Form = "inet:ipv4", Valu = "172.16.12.1" };
        var c = new SynapseNode() { Form = "inet:ipv4", Valu = "192.168.1.5" };
        var d = new SynapseNode() { Form = "inet:ipv4", Valu = "8.8.8.8" };
        await p.Process(new[] { a, b, c, d });
        
        Assert.That(a.Tags, Does.ContainKey("net.priv"));
        Assert.That(b.Tags, Does.ContainKey("net.priv"));
        Assert.That(c.Tags, Does.ContainKey("net.priv"));
        Assert.That(d.Tags, Does.Not.ContainKey("net.priv"));
    }
}