using Ganss.Xss;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DocIntel.Services.Newsletters;

public static class HtmlHelper
{
    public static IHtmlContent Sanitize(this IHtmlHelper htmlHelper,
        string text)
    {
        return new HtmlString(new HtmlSanitizer().Sanitize(text));
    }

}