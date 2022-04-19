using System.Text;

namespace DocIntel.Core.Utils.Observables;

public class LigatureRemover : ITextTransform
{
    public string Transform(string content)
    {
        var sb = new StringBuilder(content);
        sb.Replace("Ꜳ", "AA");
        sb.Replace("ꜳ", "AA");
        sb.Replace("Æ", "AE");
        sb.Replace("æ", "ae");
        sb.Replace("Ꜵ", "AO");
        sb.Replace("ꜵ", "ao");
        sb.Replace("Ꜷ", "AJ");
        sb.Replace("ꜷ", "aj");
        sb.Replace("Ꜹ", "AV");
        sb.Replace("ꜹ", "av");
        sb.Replace("Ꜻ", "AV");
        sb.Replace("ꜻ", "av");
        sb.Replace("Ꜽ", "AY");
        sb.Replace("ꜽ", "ay");
        sb.Replace("ﬀ", "ff");
        sb.Replace("ﬃ ", "ffi");
        sb.Replace("ﬄ", "ffl");
        sb.Replace("ﬁ", "fi");
        sb.Replace("ﬂ", "fl");
        sb.Replace("Œ", "OE");
        sb.Replace("œ", "oe");
        sb.Replace("Ꝏ", "OO");
        sb.Replace("ꝏ", "oo");
        sb.Replace("ﬆ", "st");
        sb.Replace("ﬅ", "ft");
        sb.Replace("Ꜩ", "TZ");
        sb.Replace("ꜩ", "tz");
        sb.Replace("ᵫ", "ue");
        sb.Replace("Ꝡ", "VY");
        sb.Replace("ꝡ", "vy");
        sb.Replace("…", "...");
        return sb.ToString();
    }
}