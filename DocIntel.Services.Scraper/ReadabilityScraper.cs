/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Razor;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Scrapers;
using DocIntel.Core.Settings;

using Ganss.XSS;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PuppeteerSharp;

using RazorLight;

namespace DocIntel.Services.Scraper
{
    [Scraper("3e13fd97-6ee2-45d4-a567-77e318589571",
        Name = "Readability",
        Description = "Scraper for websites using readability",
        Patterns = new[]
        {
            @"https?://.*"
        })]
    public class ReadabilityScraper : DefaultScraper
    {
        private readonly ILogger<ReadabilityScraper> _logger;
        private readonly Core.Models.Scraper _scraper;

        private readonly ApplicationSettings _settings;
        private RazorLightEngine _engine;

        public ReadabilityScraper(Core.Models.Scraper scraper, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _scraper = scraper;
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _logger = (ILogger<ReadabilityScraper>) serviceProvider.GetService(typeof(ILogger<ReadabilityScraper>));
        }

        public override async Task<bool> Scrape(SubmittedDocument message)
        {
            _engine = new CustomRazorLightEngineBuilder()
                .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Views"))
                .UseMemoryCachingProvider()
                .Build();

            _logger.LogDebug("Readability Scraper for " + message.URL);
            var context = GetContext(message.SubmitterId);
            var uri = new Uri(message.URL);
            var host = uri.Host;

            var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
            {
                SourceUrl = uri.ToString()
            });
            if (exists)
            {
                Console.WriteLine("Document already exists");
                _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                    SubmissionStatus.Duplicate);
                await context.DatabaseContext.SaveChangesAsync();
                return false;
            }

            _logger.LogDebug("Scraping " + uri);
            Browser browser = null;
            Page page = null;

            try
            {
                var sanitizer = new HtmlSanitizer
                {
                    KeepChildNodes = true
                };
                var hardSanitizer = new HtmlSanitizer
                {
                    KeepChildNodes = true
                };
                hardSanitizer.AllowedTags.Clear();
                _logger.LogDebug("Scrape with a browser");

                // This is needed in order to avoid the component to NOT be able to communicate with the browser
                // I know, it's silly...
                WebRequest.DefaultWebProxy = null;
                var options = new LaunchOptions
                {
                    Headless = true,
                    IgnoreHTTPSErrors = true,
                };
                
                if (!string.IsNullOrEmpty(_settings.Proxy))
                {
                    options.Args = new[]
                    {
                        "--proxy-server=\"http=" + _settings.Proxy + ";https=" + _settings.Proxy + "\""
                    };
                }
                
                _logger.LogDebug("Browser: " + options.ExecutablePath);
                _logger.LogDebug("Proxy used for executable: " + string.Join(" ", options.Args));

                browser = await Puppeteer.LaunchAsync(options);
                _logger.LogDebug("Browser launched");

                page = await browser.NewPageAsync();
                _logger.LogDebug("New page");
                var readabilityScript = File.ReadAllText("Readability.js");
                _logger.LogDebug("Go to URL: " + uri);
                var res = await page.GoToAsync(uri.ToString(), new NavigationOptions
                {
                    Timeout = 0,
                    WaitUntil = new[] {WaitUntilNavigation.DOMContentLoaded, WaitUntilNavigation.Networkidle0}
                });
                _logger.LogDebug("Loaded: " + res.Status);
                foreach (var header in res.Headers) _logger.LogDebug("Header " + header.Key + " = " + header.Value);

                var enUS = new CultureInfo("en-US");
                DateTime lastModifiedDate;
                var date = await page.EvaluateFunctionAsync<string>("() => document.lastModified");
                if (!DateTime.TryParseExact(date, "MM/dd/yyyy hh:mm:ss", enUS, DateTimeStyles.None,
                    out lastModifiedDate))
                    lastModifiedDate = DateTime.Now;

                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1280,
                    Height = 768
                });
                var pageHeight = await page.EvaluateFunctionAsync<int>("() => document.body.scrollHeight");
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1280,
                    Height = pageHeight
                });

                var screenshotFilename = Path.GetTempFileName();
                await page.ScreenshotAsync(screenshotFilename);

                await page.EvaluateExpressionAsync(readabilityScript);

                var extractedContent =
                    await page.EvaluateFunctionAsync<ReadabilityArticle>("() => new Readability(document).parse()");

                var source = _sourceRepository.GetAllAsync(context, new SourceQuery
                {
                    HomePage = host
                }).ToEnumerable().FirstOrDefault() ?? await _sourceRepository.CreateAsync(context, new Source
                {
                    Title = string.IsNullOrEmpty(extractedContent.SiteName) ? host : extractedContent.SiteName,
                    HomePage = host
                });

                var d = new Document
                {
                    Title = string.IsNullOrEmpty(message.Title) ? extractedContent.Title : message.Title,
                    ShortDescription =
                        hardSanitizer.Sanitize(string.IsNullOrEmpty(message.Description)
                            ? extractedContent.Excerpt
                            : message.Description),
                    SourceId = source.SourceId,
                    Status = DocumentStatus.Submitted,
                    ClassificationId = _classificationRepository.GetDefault(context).ClassificationId,
                    DocumentDate = message.SubmissionDate
                };
                var trackingD = await _documentRepository.AddAsync(context, d, new Tag[] { });
                _logger.LogDebug("Document created...");

                var screenshot = new DocumentFile
                {
                    Document = trackingD,
                    MimeType = "image/png",
                    Filename = d.DocumentId + "-story-screenshot.png",
                    Title = "Original story (screenshot)",
                    ClassificationId = _classificationRepository.GetDefault(context)?.ClassificationId,
                    DocumentDate = lastModifiedDate,
                    SourceUrl = message.URL,
                    Visible = true,
                    Preview = true
                };

                await using var screenshotStream = new FileStream(screenshotFilename, FileMode.Open);
                screenshot = await _documentRepository.AddFile(context, screenshot, screenshotStream);
                File.Delete(screenshotFilename);
                _logger.LogDebug("Screenshot captured...");

                extractedContent.Content = sanitizer.Sanitize(extractedContent.Content);

                var documentFile = new DocumentFile
                {
                    Document = trackingD,
                    MimeType = "text/html",
                    Filename = d.DocumentId + "-story" + ".html",
                    Title = "Original story",
                    ClassificationId = _classificationRepository.GetDefault(context).ClassificationId,
                    DocumentDate = message.SubmissionDate,
                    SourceUrl = message.URL,
                    Visible = true,
                    Preview = true
                };

                var pageHTML = await _engine.CompileRenderAsync("Scraper/Readability.cshtml", extractedContent);

                await using var extractedStream = new MemoryStream();
                extractedStream.Write(Encoding.UTF8.GetBytes(pageHTML));
                extractedStream.Position = 0;
                documentFile = await _documentRepository.AddFile(context, documentFile, extractedStream);
                _logger.LogDebug("HTML extracted...");

                _logger.LogDebug("Closing Browser...");
                await page.CloseAsync();
                await browser.CloseAsync();
                _logger.LogDebug("Closed...");

                _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
                _logger.LogDebug("Saving changes...");
                await context.DatabaseContext.SaveChangesAsync();
                _logger.LogDebug("Saved changes...");
            }
            catch (NavigationException e)
            {
                Console.WriteLine("---- Could not scrape content");
                Console.WriteLine(e.Url);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("----");
                Console.WriteLine(e.InnerException);
                Console.WriteLine("----");
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine("---- Could not scrape content");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("----");
                Console.WriteLine(e.InnerException);
                Console.WriteLine("----");
            }
            catch (Exception e)
            {
                // TODO Do NOT catch Exception, be more specific
                Console.WriteLine("---- Could not scrape content");
                Console.WriteLine(e.GetType().FullName);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("----");
                Console.WriteLine(e.InnerException);
                Console.WriteLine("----");
            }
            finally
            {
                if (page != null)
                    await page.CloseAsync();
                if (browser != null) await browser.CloseAsync();
                _logger.LogDebug("Closed (2)...");
            }

            return false;
        }
    }
}
