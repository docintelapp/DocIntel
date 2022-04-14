// using System;
// using System.IO;
// using System.Text.Json;
// using DocIntel.Core.Models;
// using DocIntel.Core.Repositories;
// using Microsoft.Extensions.Logging;
// using System.Linq;
// using System.Threading.Tasks;
// using BlushingPenguin.JsonPath;
// using System.Net;
// using System.Text;
// using DocIntel.Core.Settings;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.AspNetCore.Identity;

// namespace DocIntel.Services.Importer
// {
//     public class FireEyeFolderImporter : FireEyeImporter
//     {
//         private Settings _pluginSettings;
        
//         private string json_cache;

//         private string last_reference_pulled;
//         private FireEyeAPI client;

//         public class Settings {
//             public string username { get; set; }
//             public string api_key { get; set; }
//             public string secret { get; set; }
//             public string last_reference_pulled { get; set; }
//             public string proxy { get; set; }
//         }

//         public FireEyeFolderImporter(
//             ILogger<FireEyeImporter> logger,
//             ISourceRepository sourceRepository,
//             IUserRepository userRepository,
//             IIncomingFeedRepository pluginRepository,
//             ApplicationSettings settings,
//             IServiceProvider serviceProvider,
//             IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
//             ILogger<DocIntelContext> contextLogger)
//             : base(logger,
//                    sourceRepository,
//                    userRepository,
//                    pluginRepository,
//                    settings,
//                    Program.PluginId,
//                    serviceProvider,
//                    userClaimsPrincipalFactory,
//                    contextLogger)
//         {
//         }

//         public async Task RunAsync(string[] args) {
//             await InitAsync();

//             if (_plugin.Enabled) {
//                 RunOptionsAndReturnExitCodeAsync(args).Wait();
//             } else {
//                 System.Console.WriteLine("Plugin is not enabled.");
//             }
//         }
    
//         private async Task RunOptionsAndReturnExitCodeAsync(string[] args)
//         {   
//             var files = Directory.GetFiles(args[1], args[2]);
//             foreach (var file in files) {
//                 var reportFilename = Path.GetFileNameWithoutExtension(file);
//                 var pdf = Path.Combine(args[1], reportFilename + ".pdf");
//                 if (File.Exists(pdf)) {
//                     var content = File.ReadAllText(file);
//                     var jsonDocument = JsonDocument.Parse(content);
//                     using (FileStream pdfStream = File.OpenRead(pdf)) {
//                         var doc = await ImportReportAsync(user, source, jsonDocument, pdfStream);
//                         if (doc != null)
//                             _logger.LogInformation("Imported " + doc.ExternalReference);
//                     }
//                 } else {
//                     _logger.LogWarning($"Ignoring {reportFilename} because PDF could not be found.");
//                 }
//             }
//         }
//     }
// }