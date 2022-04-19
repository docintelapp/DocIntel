using DocIntel.Core.Utils.Features;
using Microsoft.Extensions.Logging;

namespace DocIntel.Tests;

public class TestExtractCVE
{
    [Test]
    public void TestExtractCVENames()
    {
        var text = @"Briefly, the exploitation of the CVE-2022-30190 vulnerability can be described as follows. The 
attacker creates an MS Office document with a link to an external malicious OLE object ( word/_rels/document.xml.rels ), 
such as an HTML file located on a remote server. The data used to describe the link is placed in the tag with attributes 
Type=”http://schemas.openxmlformats.org/officeDocument/2006/relationships/oleObject”, Target=”http_malicious_link!” . 
The link in the Target attribute points to the above-mentioned HTML file, inside which a malicious script is written 
using a special URI scheme.";
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var extractor = new CVEFeatureExtractor();
        var cve = extractor.Extract(text);

        Console.WriteLine(string.Join(",", cve));
        
        Assert.That(cve.Count(), Is.EqualTo(1));
        Assert.That(cve, Contains.Item("CVE-2022-30190"));
    }

    [Test]
    public void TestExtractCVEFromCISAReport()
    {
        var text = @"Primary
Vendor --
Product
Description Published CVSS
Score
Source &
Patch Info
adobe --
photoshop
Adobe Photoshop versions 22.5.6 (and earlier)and 23.2.2 (and
earlier) are affected by an out-of-bounds write vulnerability that
could result in arbitrary code execution in the context of the current
user. Exploitation of this issue requires user interaction in that a
victim must open a malicious file.
2022-05-06 9.3 CVE-2022-23205
MISC
adobe --
photoshop
Adobe Photoshop versions 22.5.6 (and earlier)and 23.2.2 (and
earlier) are affected by an improper input validation vulnerability
when parsing a PCX file that could result in arbitrary code execution
in the context of the current user. Exploitation of this issue requires
user interaction in that a victim must open a malicious PCX file.
2022-05-06 9.3 CVE-2022-24098
MISC
adobe --
photoshop
Adobe Photoshop versions 22.5.6 (and earlier)and 23.2.2 (and
earlier) are affected by an out-of-bounds write vulnerability that
could result in arbitrary code execution in the context of the current
user. Exploitation of this issue requires user interaction in that a
victim must open a malicious U3D file.
2022-05-06 9.3 CVE-2022-24105
MISC ";
        
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole(options => options.DisableColors = true);
        });

        var extractor = new CVEFeatureExtractor();
        var cve = extractor.Extract(text);

        Console.WriteLine(string.Join(",", cve));
        
        Assert.That(cve.Count(), Is.EqualTo(3));
        Assert.That(cve, Contains.Item("CVE-2022-23205"));
        Assert.That(cve, Contains.Item("CVE-2022-24098"));
        Assert.That(cve, Contains.Item("CVE-2022-24105"));
    }
    
}