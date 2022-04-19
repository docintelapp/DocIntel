using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.Core.Utils.Observables;

public class RegexDomainExtractor : RegexExtractor
{
    private ILogger<RegexDomainExtractor> _logger;
    private readonly HashSet<string> _TLD;

    public RegexDomainExtractor(ILogger<RegexDomainExtractor> logger)
    {
        _logger = logger;
        
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "DocIntel.Core.tld.json";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            if (stream != null)
            {
                using StreamReader reader = new StreamReader(stream);
                _TLD = new HashSet<string>(JsonConvert.DeserializeObject<List<string>>(reader.ReadToEnd()) ?? Enumerable.Empty<string>());
            }
            else 
                _TLD = new HashSet<string>();
    }

    // Currently not matching unicode characters, due to high false positive rates when auto-extracting
    public const string REGEX_DOMAIN_REGEX = @"
            # see if preceded by slashes or @
            (\/|\\|@|@\]|%2F)? 
            (           
            (?:  
                [a-zA-Z\d-]{1,63}  # Alphanumeric chunk (also dashes)
            (?:\.|" + DEFANGED_DOT_REGEX + @")             # Dot separator between labels.
                ){1,63}
            
                [a-zA-Z]{2,}  # Top level domain (numbers excluded)
            )
            ";

#pragma warning disable CS1998
    public override async IAsyncEnumerable<SynapseObject> Extract(string content)
#pragma warning restore CS1998
    {       
        // Only extract domains that are followed by a whitespace elements
        // REVIEW Can't we add all the punctuation marks? To be tested and further validated wrt false positive rate. 
        var matches = Regex.Matches(content, REGEX_DOMAIN_REGEX + @"(?=[\s\n\r\t\v\f|$])", DEFAULT_REGEX_OPTIONS);

        foreach (Match capture in matches)
        {
            // The regex will return the following groups:
            // 0. Complete match
            // 1. First slash
            // 2. The FQDN 
            // 3. The rest of the path
            
            // REVIEW Unclear what this line intends to achieve. Please document the rationale.
            if (!string.IsNullOrEmpty(capture.Groups[1].Value))
                continue;

            var domain = RefangDots(capture.Groups[2].Value);
            
            var tld = domain.Substring(domain.LastIndexOf(".", StringComparison.Ordinal) + 1).ToUpper();
            if (!_TLD.Contains(tld)) 
                continue;
            
            var synapseObject = new InetFqdn();
            synapseObject.SetValue(domain);
            yield return synapseObject;
        }
    }
}