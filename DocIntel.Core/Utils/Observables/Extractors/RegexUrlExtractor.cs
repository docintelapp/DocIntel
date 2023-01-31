using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables.Extractors;

public class RegexUrlExtractor : RegexExtractor
{
    private ILogger<RegexUrlExtractor> _logger;

    public RegexUrlExtractor(ILogger<RegexUrlExtractor> logger)
    {
        _logger = logger;
    }

    public const string GenericUrlRegex = @"
        (
            # Scheme.
            [fhstu]\S\S?[px]s?
            # One of these delimiters/defangs.
            (?:
                :\/\/|
                :\\\\|
                :?__
            )
            # Any number of defang characters.
            (?:
                \x20|" + SEPARATOR_DEFANGS + @"
            )*
            # Domain/path characters.
            ([a-zA-Z0-9:\./_~!$&()*+,;=:@-]+?)
            # CISCO ESA style defangs followed by domain/path characters.
            (?:\x20[\/\.][^\.\/\s]\S*?)*
        )" + END_PUNCTUATION + @"(?=[\s""<>^`'{|}]|$)";
        
    public const string BracketUrlRegex = @"
        (
            # Scheme.
            [fhstu]\S\S?[px]s?
            # One of these delimiters/defangs.
            (?:
                :\/\/|
                :\\\\|
                :?__
            )
            # Any number of defang characters.
            (?:
                \x20|" + SEPARATOR_DEFANGS + @"
            )*
            # Domain/path characters.
            (
                [\.\:\/\\\w\[\]\(\)-]+
                (?:
                    \x20?
                    [\(\[]
                    \x20?
                    \.
                    \x20?
                    [\]\)]
                    \x20?
                    [a-zA-Z0-9\.-_~!$&()*+,;=:@]*?
                )+
            )
            # CISCO ESA style defangs followed by domain/path characters.
            (?:\x20[\/\.][^\.\/\s]\S*?)*
        )" + END_PUNCTUATION + @"(?=[\s""<>^`'{|}]|$)";

    // REVIEW Suspiciously unused field
    // ReSharper disable once UnusedMember.Local
    public const string BackslashUrlRegex = @"
        (
            # Scheme.
            [fhstu]\S\S?[px]s?
            # One of these delimiters/defangs.
            (?:
                :\/\/|
                :\\\\|
                :?__
            )
            # Any number of defang characters.
            (?:
                \x20|" + SEPARATOR_DEFANGS + @"
            )*
            # Domain/path characters.
            (
                [\:\/\\\w\[\]\(\)-]+
                (?:
                    \x20?
                    \\?\.
                    \x20?
                    \S*?
                )*?
                (?:
                    \x20?
                    \\\.
                    \x20?
                    \S*?
                )
                (?:
                    \x20?
                    \\?\.
                    \x20?
                    \S*?
                )*
            )
            # CISCO ESA style defangs followed by domain/path characters.
            (?:\x20[\/\.][^\.\/\s]\S*?)*
        )" + END_PUNCTUATION + @"(?=[\s""<>^`'{|}]|$)";

#pragma warning disable CS1998
    public override async IAsyncEnumerable<SynapseNode> Extract(string content)
#pragma warning restore CS1998
    {
        var uris = new HashSet<Uri>();
        
        var matches = Regex.Matches(content, GenericUrlRegex, DEFAULT_REGEX_OPTIONS);
        foreach (Match capture in matches)
        {
            var url = NormalizeUrl(capture.Groups[0].Value);
            _logger.LogTrace($"Extracted {url} with GENERIC_URL_REGEX ({capture.ToString()}).");
            if (url is not null) uris.Add(url);
        }

        // Appears to extract domains and IP addresses as URL. Leading to high number of false positive
        matches = Regex.Matches(content, BracketUrlRegex, DEFAULT_REGEX_OPTIONS);
        foreach (Match capture in matches)
        {
            var url = NormalizeUrl(capture.Groups[1].Value);
            _logger.LogTrace($"Extracted {url} with BRACKET_URL_REGEX ({capture.Groups[1].Value.ToString()}).");
            if (url is not null) uris.Add(url);
        }

        // REVIEW Why was it commented? Lack of testing or too high false positive rate.
        // If too high positive rate, please delete the code (and related code as well)
        matches = Regex.Matches(content, BackslashUrlRegex, DEFAULT_REGEX_OPTIONS);
        foreach (Match capture in matches)
        {
            var url = NormalizeUrl(capture.Groups[1].Value);
            _logger.LogTrace($"Extracted {url} with BACKSLASH_URL_REGEX ({capture.Groups[1].Value.ToString()}).");
            if (url is not null) uris.Add(url);
        }

        _logger.LogDebug($"Extracted {uris.Count} urls.");
        foreach (var uri in uris)
        {
            var synapseObject = new SynapseNode()
            {
                Form = "inet:url",
                Valu = uri.ToString()
            };
            yield return synapseObject;
        }
    }
    
    private Uri NormalizeUrl(string url)
    {
     _logger.LogDebug($"Normalize '{url}'");       
            // TODO Use StringBuilder in the function, and not string. As StringBuilder are for mutable strings and likely more efficient in this context.
            var mutableUrl = new StringBuilder(url);
            if (url.Contains("[.") & !url.Contains("[.]")) mutableUrl.Replace("[.", "[.]");
            if (url.Contains(".]") & !url.Contains("[.]")) mutableUrl.Replace(".]", "[.]");
            if (url.Contains("[dot") & !url.Contains("[dot]")) mutableUrl.Replace("[dot", "[.]");
            if (url.Contains("dot]") & !url.Contains("[dot]")) mutableUrl.Replace("dot]", "[.]");
            if (url.Contains("[/]")) mutableUrl.Replace("[/]", "/");
            url = mutableUrl.ToString();
                
            // Ensure a scheme exists
            if (!url.Contains("//"))
            {
                // Get the 8 first character of the url, that should contain the scheme if it exists and attempt
                // to fix the URL properly, refanging what is needed. 
                var url8 = url.Length >= 8 ? url.Substring(0, 8) : url;
                if (url8.Contains("__"))
                {
                    // Refang http__domain and http:__domain.
                    if (url8.Contains(":__"))
                        url = ReplaceFirst(url, ":__", "://");
                    else
                        url = ReplaceFirst(url, "__", "://");
                }
                else if (url8.Contains("\\\\"))
                {
                    // Refang http:\\domain
                    url = ReplaceFirst(url, "\\\\", "//");
                }
                else
                {
                    // Refang no-protocol
                    url = "http://" + url;
                }
            }

            // Refang (/) and  some backslash-escaped characters.
            mutableUrl = new StringBuilder(url);
            mutableUrl.Replace("(/)", "/");
            mutableUrl.Replace(@"\.", ".");
            mutableUrl.Replace(@"\(", "(");
            mutableUrl.Replace(@"\[", "[");
            mutableUrl.Replace(@"\)", ")");
            mutableUrl.Replace(@"\]", "]");

            // Refang dots, or Uri won't parse.
            RefangDots(mutableUrl);

            // TODO Avoid enclosing a try/catch block in other, both catching Exception. Use more specific Exception. 
            try
            {
                var uri = new Uri(mutableUrl.ToString());

                try
                {
                    var uriBuilder = new UriBuilder(uri);

                    // Handle URLs with no scheme / obfuscated scheme.
                    if (!new[] {"http", "https", "ftp"}.Contains(uri.Scheme))
                    { 
                        if (new[] {"ftxs", "fxps"}.Contains(uri.Scheme))
                            uriBuilder.Scheme = "ftps";
                        else if (new[] {"ftx", "fxp"}.Contains(uri.Scheme))
                            uriBuilder.Scheme = "ftp";
                        else if (new[] {"hxxps"}.Contains(uri.Scheme))
                            uriBuilder.Scheme = "https";
                        else if (new[] {"hxxp"}.Contains(uri.Scheme))
                            uriBuilder.Scheme = "http";
                        else
                        {
                            _logger.LogDebug($"Unrecognize scheme: {uri.Scheme}, defaulting to HTTP.");
                            uriBuilder.Scheme = "http";
                        }
                    }

                    // Remove artifacts from common defangs.
                    uriBuilder.Host = NormalizeCommon(uriBuilder.Host);
                    uriBuilder.Path = RefangDots(uriBuilder.Path);

                    // Fix example[.]com, but keep IPv6 URLs (see RFC 2732 for details) intact.
                    // IPv6 URLs look like http://[::FFFF:129.144.52.38]:80/index.html
                    if (!IsIPv6Url(uriBuilder.Uri))
                        uriBuilder.Host = uriBuilder.Host.Replace("[", "").Replace("]", "");
                    
                    _logger.LogDebug($"Normalized as '{uriBuilder.Uri}'");
                    return uriBuilder.Uri;
                }
                catch (Exception e)
                {
                    // TODO Provide a meaningful error message, and do NOT catch Exception.
                    // TODO Please be more specific, otherwise, let it go higher in the call stack.
                    _logger.LogError($"Error urlbuilder '{url}': {e.Message}");
                }
            }
            catch (Exception e)
            {
                // TODO Provide a meaningful error message, and do NOT catch Exception.
                // TODO Please be more specific, otherwise, let it go higher in the call stack.
                _logger.LogWarning($"Could not convert to uri '{url}': {e.Message}");
                // Last resort on ipv6 fail.
                // uri = new Uri(url.Replace("[", "").Replace("]", ""));
            }

            return null;
        }
    
    private bool IsIPv6Url(Uri parsed)
    {
        string ipv6;
        // Handle RFC 2732 IPv6 URLs with and without port, as well as non-RFC IPv6 URLs.
        if (parsed.Host.Contains("]:"))
            ipv6 = string.Join(':', parsed.Host.Split(':').Reverse().Skip(1).Reverse());
        else
            ipv6 = parsed.Host;

        if (IPAddress.TryParse(ipv6, out var address))
            switch (address.AddressFamily)
            {
                case AddressFamily.InterNetworkV6:
                    return true;
                default:
                    return false;
            }

        return false;
    }
}