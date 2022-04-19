using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Synsharp;

namespace DocIntel.Core.Utils.Observables;

public abstract class RegexExtractor : IExtractor
{
    public const string END_PUNCTUATION = @"[\.\?>""'\)!,}:;\u201d\u2019\uff1e\uff1c\]]*";

    public const string SEPARATOR_DEFANGS = @"[\(\)\[\]{}<>\\]";

    public const string DEFANGED_DOT_REGEX = @"\.|\[\.\]|\(\.\)|\sDOT\s|\[dot\]|\[DOT\]|\(dot\)|\(DOT\)";
        
    protected const RegexOptions DEFAULT_REGEX_OPTIONS =
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
        
    protected static string RefangDots(string url)
    {
        var sb = new StringBuilder(url);
        RefangDots(sb);
        return sb.ToString();
    }

    protected static void RefangDots(StringBuilder sb)
    {
        sb.Replace("[.]", ".");
        sb.Replace("(.)", ".");
        sb.Replace(" DOT ", ".");
        sb.Replace("[DOT]", ".");
        sb.Replace("[dot]", ".");
        sb.Replace("(DOT)", ".");
        sb.Replace("(dot)", ".");
    }
        
    protected static string ReplaceFirst(string text, string search, string replace)
    {
        var pos = text.IndexOf(search);
        if (pos < 0) return text;
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }

    protected static string NormalizeCommon(string ioc)
    {
        var sb = new StringBuilder(ioc);
        NormalizeCommon(sb);
        return sb.ToString();
    }

    protected static void NormalizeCommon(StringBuilder sb)
    {
        RefangDots(sb);
        sb.Replace("(", "");
        sb.Replace(")", "");
        sb.Replace(",", ".");
        sb.Replace(" ", "");
        sb.Replace("\u30fb", ".");
    }

    public abstract IAsyncEnumerable<SynapseObject> Extract(string content);
}