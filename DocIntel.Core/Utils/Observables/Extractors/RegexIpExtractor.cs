using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.Core.Utils.Observables;

public class RegexIpExtractor : RegexExtractor
{
    public const string IPV4_REGEX = @"
            (?:^|
                (?![^\d\.])
            )
            (?:
                (?:[1-9]?\d|1\d\d|2[0-4]\d|25[0-5])
                [\[\(\\]*?\.[\]\)]*?
            ){3}
            (?:[1-9]?\d|1\d\d|2[0-4]\d|25[0-5])
            (?:(?=[^\d\.])|$)
        ";

    // TODO The following regex generates a too high false positive rate.
    // ReSharper disable once UnusedMember.Local
    public const string IPV6_REGEX = @"
            \b(?:[a-f0-9]{1,4}:|:){2,7}(?:[a-f0-9]{1,4}|:)\b
        ";
    
#pragma warning disable CS1998
    public override async IAsyncEnumerable<SynapseObject> Extract(string content)
#pragma warning restore CS1998
    {
        var options = DEFAULT_REGEX_OPTIONS | RegexOptions.Compiled;
        var matches = Regex.Matches(content, IPV4_REGEX, options);
        foreach (Match match in matches)
        {
            var synapseObject = new InetIPv4();
            synapseObject.SetValue(RefangIPv4(match.Groups[0].Value));
            yield return synapseObject;
        }

        // TODO Check if CIDR ranges are properly extracted, otherwise add proper extraction.
    }

    private string RefangIPv4(string ip)
    {
        var mutableIPAddress = new StringBuilder(ip);
        NormalizeCommon(mutableIPAddress);
        mutableIPAddress.Replace("[", "");
        mutableIPAddress.Replace("]", "");
        mutableIPAddress.Replace(@"\\", "");
        return mutableIPAddress.ToString();
    }
}