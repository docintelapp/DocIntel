using System.ComponentModel.DataAnnotations;

namespace DocIntel.WebApp.Areas.API.Models;

/// <summary>
/// A comment
/// </summary>
public class ApiComment
{
    /// <summary>
    /// The body, in HTML, of the comment.
    /// </summary>
    /// <example><![CDATA[<p>This is my comment.</p>]]></example>
    [Required]
    public string Body { get; set; }
}