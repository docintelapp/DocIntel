using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocIntel.Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Synsharp.Telepath;

namespace DocIntel.Core.Helpers;

public static class FlightChecks
{
    public static async Task<int> PreFlightChecks()
    {
        Console.WriteLine("Running pre-flight checks...");
            
        if (!CheckNlogConfig(out var nlogfilepath))
        {
            Console.WriteLine("DocIntel could not file an appropriate log configuration file. " +
                              "Please check that you have a nlog.config file in the current directory, " +
                              "/config/ or /etc/docintel/");
            return 1;
        }
        else
        {
            Console.WriteLine($"[OK] Log configuration file found: {nlogfilepath}");
        }
            
        if (!CheckAppSettingsConfig(out var appSettingsFilePath))
        {
            Console.WriteLine("DocIntel could not file an appropriate app configuration file. " +
                              "Please check that you have a appsettings.json file in the current directory, " +
                              "/config/ or /etc/docintel/");
            return 1;
        }
        else
        {
            Console.WriteLine($"[OK] App configuration file found: {appSettingsFilePath}");
        }

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());

        if (File.Exists("appsettings.json"))
            configurationBuilder.AddJsonFile("appsettings.json");
        else if (File.Exists("/config/appsettings.json"))
            configurationBuilder.AddJsonFile("/config/appsettings.json");
        else if (File.Exists("/etc/docintel/appsettings.json"))
            configurationBuilder.AddJsonFile("/etc/docintel/appsettings.json");

        var configuration = configurationBuilder.Build();
        var applicationSettings = new ApplicationSettings();
        configuration.Bind(applicationSettings);

        Console.WriteLine($"[OK] Authentication method: {applicationSettings.AuthenticationMethod}");

        var diskSpaceSuccess = await DiskPreFlightChecks(applicationSettings);
        
        var proxySuccess = ProxyPreFlightChecks(applicationSettings);
        var solrSuccess = await SolRPreFlightChecks(applicationSettings.Solr);
        var synapseSuccess = await SynapsePreFlightChecks(applicationSettings.Synapse);
        var rabbitMqSuccess = await RabbitMqPreFlightChecks(applicationSettings.RabbitMQ);
        var postgresSuccess = await PostgresPreFlightChecks(configuration);
        var emailSuccess = await EmailPreFlightChecks(applicationSettings.Email);
        
        if (diskSpaceSuccess & proxySuccess & solrSuccess & synapseSuccess & rabbitMqSuccess & postgresSuccess)
        {
            Console.WriteLine("[OK] All checks are ok, we are good to start.");
            return 0;
        }
        else
        {
            Console.WriteLine("[KO] Not all checks passed, aborting. Please check the errors, fix the problems, and restart.");
            return 1;
        }
    }
    
    private static bool CheckNlogConfig(out string filePath)
    {
        string[] possiblePaths = new string[]
        {
            "nlog.config",  // Check in current directory
            "/config/nlog.config",  // Check in /config directory
            "/etc/docintel/nlog.config"  // Check in /etc/docintel directory
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                filePath = Path.GetFullPath(path);
                return true;
            }
        }

        filePath = null;
        return false;
    }

    private static bool CheckAppSettingsConfig(out string filePath)
    {
        string[] possiblePaths = new string[]
        {
            "appsettings.json",  // Check in current directory
            "/config/appsettings.json",  // Check in /config directory
            "/etc/docintel/appsettings.json"  // Check in /etc/docintel directory
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                filePath = Path.GetFullPath(path);
                return true;
            }
        }

        filePath = null;
        return false;
    }

    private static async Task<bool> PostgresPreFlightChecks(IConfigurationRoot config)
    {
        Console.WriteLine("---- Running pre-flight checks for Postgres...");
        var ret = true;
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine($"[OK] DocIntel will use the DefaultConnection connection string.");
        }
        else
        {
            Console.WriteLine($"[KO] DefaultConnection connection string is null or empty.");
            ret = false;
        }
        
        if (ret)
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                Console.WriteLine("[OK] DocIntel could connect to PostgreSQL server.");

                try
                {
                    using var command = new NpgsqlCommand(
                        @"SELECT ""MigrationId"" FROM ""__EFMigrationsHistory"" ORDER BY ""MigrationId"" DESC LIMIT 1", conn);

                    using var reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        var latestMigrationId = reader.GetString(0);
                        Console.WriteLine($"[OK] Latest migration ID: {latestMigrationId}");
                    }
                }
                catch
                {
                    Console.WriteLine($"[??] Could not retrieve last migrations, may be running for the first time?");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KO] DocIntel could not connect to PostgreSQL server ({ex.Message})");
                ret = false;
            }
        }

        return ret;
    }

    private static async Task<bool> RabbitMqPreFlightChecks(RabbitMQSettings settings)
    {
        Console.WriteLine("---- Running pre-flight checks for RabbitMQ...");
        bool ret = true;
        
        if (!string.IsNullOrEmpty(settings.Host))
        {
            Console.WriteLine($"[OK] DocIntel will use the RabbitMQ server located at '{settings.Host}'.");
            Console.WriteLine($"[OK] DocIntel will log in on RabbitMQ with '{settings.Username}' username.");
        }
        else
        {
            Console.WriteLine($"[KO] RabbitMQ URI is null or empty.");
            ret = false;
        }

        if (ret)
        {
            try
            {
                using var httpClient = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}");
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await httpClient.GetAsync($"http://{settings.Host}:15672/api/overview");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[KO] DocIntel could not connect to RabbitMQ server.");
                    ret = false;
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    Console.WriteLine(
                        $"[KO] DocIntel received an empty response from RabbitMQ server. Please check that RabbitMQ is up and running.");
                    ret = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[!!] DocIntel could not reach RabbitMQ server ({e.Message}). " +
                                  $"Check that rabbitmq_management plugin is installed, for example with " +
                                  $"docker exec -it <name-of-rabbitmq-container> rabbitmq-plugins enable rabbitmq_management ");
            }
            
        }

        if (ret)
            Console.WriteLine($"[OK] DocIntel could reach and connect to RabbitMQ.");

        return ret;
    }

    private static async Task<bool> DiskPreFlightChecks(ApplicationSettings applicationSettings)
    {
        Console.WriteLine(Directory.Exists(applicationSettings.ModulesFolder)
            ? $"[OK] Module folder: {applicationSettings.ModulesFolder}"
            : $"[KO] Module folder '{applicationSettings.ModulesFolder}' not found.");
        
        return CheckFolder(applicationSettings.DocFolder);
    }

    private static bool CheckFolder(string folderPath)
    {
        Console.WriteLine("---- Running pre-flight checks for the disk...");
            
        bool ret = true;
        string driveName = new DirectoryInfo(folderPath).Root.Name;
        DriveInfo drive = new DriveInfo(driveName);

        // Check if folder is writable
        if ((File.GetAttributes(folderPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            Console.WriteLine($"[KO] The folder {folderPath} is read-only.");
            ret = false;
        }
        else
        {
            Console.WriteLine($"[OK] The folder {folderPath} is writable.");
        }

        // Check if there is at least 5% free disk space
        double freeSpacePercent = (double)drive.AvailableFreeSpace / (double)drive.TotalSize * 100.0;
        if (freeSpacePercent < 5.0)
        {
            Console.WriteLine($"[KO] There is not enough free disk space on the partition {driveName}: {freeSpacePercent:0.##}");
            ret = false;
        } else 
            Console.WriteLine($"[OK] There is at least 5% of free disk space.");
            
        return ret;
    }

    private static bool ProxyPreFlightChecks(ApplicationSettings applicationSettings)
    {
        var ret = true;

        Console.WriteLine("---- Running pre-flight checks for proxy...");
        if (!string.IsNullOrEmpty(applicationSettings.Proxy))
        {
            Uri uri;
            if (Uri.TryCreate(applicationSettings.Proxy, UriKind.Absolute, out uri) 
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                Console.WriteLine($"[OK] Proxy URL is valid");

                try
                {
                    var tcpClient = new TcpClient();
                    tcpClient.Connect(uri.Host, uri.Port);
                    Console.WriteLine($"[OK] Proxy server {uri.Host} is reachable on port {uri.Port}");
                }
                catch (Exception e)
                {
                    ret = false;
                    Console.WriteLine("[KO] Proxy server could not be reached.");   
                }
            }
            else
            {
                ret = false;
                Console.WriteLine("[KO] Proxy URI is invalid. Specify a correct URI to the Proxy server.");   
            }
        }
        else
        {
            Console.WriteLine($"[OK] No proxy specified.");   
        }

        return ret;
    }

    private static async Task<bool> SolRPreFlightChecks(SolrSettings solrSettings)
    {
        var ret = true;

        Console.WriteLine("---- Running pre-flight checks for SolR...");

        if (string.IsNullOrEmpty(solrSettings.Uri))
        {
            ret = false;
            Console.WriteLine("[KO] SolrR URI is null or empty. Specify a correct URI to the SolR server.");
        }
        else
        {
            Console.WriteLine($"[OK] DocIntel will use the SolR server located at {solrSettings.Uri}");
        }

        Uri uri;
        if (Uri.TryCreate(solrSettings.Uri, UriKind.Absolute, out uri) 
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            Console.WriteLine($"[OK] SolrR URL is valid");

            try
            {
                var tcpClient = new TcpClient();
                tcpClient.Connect(uri.Host, uri.Port);
                Console.WriteLine($"[OK] SolrR server {uri.Host} is reachable on port {uri.Port}");
            }
            catch (Exception e)
            {
                ret = false;
                Console.WriteLine("[KO] SolrR server could not be reached.");   
            }
        }
        else
        {
            ret = false;
            Console.WriteLine("[KO] SolrR URI is invalid. Specify a correct URI to the SolrR server.");   
        }
            
        if (ret)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(solrSettings.Uri + "/solr/admin/info/system?wt=json");
                string apiResponse = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(apiResponse);
                var lucene = doc.RootElement.GetProperty("lucene");
                string solrSpecVersion = lucene.GetProperty("solr-spec-version").GetString();
                string luceneSpecVersion = lucene.GetProperty("lucene-spec-version").GetString();
                string solrHome = doc.RootElement.GetProperty("solr_home").GetString();

                Console.WriteLine($"[OK] DocIntel successfully connected to SolR (Solr: {solrSpecVersion}, Lucene: {luceneSpecVersion})");
            }
            catch (Exception e)
            {
                ret = false;
                Console.WriteLine($"[KO] DocIntel could not connect to the SolR server ({e.Message}).");   
            }
                
        }
            
        if (ret)
        {
            await CheckSolRCore(solrSettings, "document", "dd8797b6467657af39021615b51aba22", "afa181ec19bb965f95e55871b2748d86");
            await CheckSolRCore(solrSettings, "tag", "2352416bd9b7f609fda9aa5ce9429005", "15af4bbd0222eea195307519b469592f");
            await CheckSolRCore(solrSettings, "facet", "df64ee714a333057d0a5ebaf0d50f984", "dbfd0e73a69141eb2613f8b33fea71e3");
            await CheckSolRCore(solrSettings, "source", "0c50f43a645c5694733359ec9d9731c5", "15af4bbd0222eea195307519b469592f");
        }
            
        return ret;
    }

    private static async Task CheckSolRCore(SolrSettings solrSettings, string core, string expectedHashSchema, string expectedHashConfig)
    {
        try
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(solrSettings.Uri + $"/solr/{core}/admin/ping?wt=json");
                response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP response status code is not in the 2xx range
                Console.WriteLine($"[OK] Solr core '{core}' is up and running.");

                await CheckSolRFileHash(client, solrSettings, core, "managed-schema.xml", expectedHashSchema);
                await CheckSolRFileHash(client, solrSettings, core, "solrconfig.xml", expectedHashConfig);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[KO] Error checking Solr core '{core}': {ex.Message}");
        }
    }

    private static async Task CheckSolRFileHash(HttpClient client, SolrSettings solrSettings, string core, string filename,
        string expectedHash)
    {
        var url = solrSettings.Uri + $"/solr/{core}/admin/file?file={filename}";
        // Download the schema and config as a byte array
        var schemaBytes = await client.GetByteArrayAsync(url);
        // Compute the MD5 hash of the schema and config
        var md5 = MD5.Create();
        var hash = md5.ComputeHash(schemaBytes);
        // Convert the hash to a string
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();

        if (hashString == expectedHash)
        {
            Console.WriteLine($"[OK] The {filename} for SolR core '{core}' is up to date.");
        }
        else
        {
            Console.WriteLine(
                $"[KO] The {filename} for SolR core '{core}' is not up to date. Please update the {filename} file.");
        }
    }

    private static async Task<bool> SynapsePreFlightChecks(SynapseSettings synapseSettings)
    {
        var ret = true;
            
        Console.WriteLine("---- Running pre-flight checks for Synapse...");

        if (string.IsNullOrEmpty(synapseSettings.URL))
        {
            ret = false;
            Console.WriteLine("[KO] Synapse URI is null or empty. Specify a correct URI to the Synapse server.");
        }
        else
        {
            Console.WriteLine($"[OK] DocIntel will use the Synapse server located at '{synapseSettings.URL}'.");
            Console.WriteLine($"[OK] DocIntel will log in on Synapse with '{synapseSettings.UserName}' username.");
        }

        Uri uri;
        if (Uri.TryCreate(synapseSettings.URL, UriKind.Absolute, out uri) 
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeNetTcp))
        {
            Console.WriteLine($"[OK] Synapse URL is valid.");

            try
            {
                var tcpClient = new TcpClient();
                tcpClient.Connect(uri.Host, uri.Port);
                Console.WriteLine($"[OK] Synapse server {uri.Host} is reachable on port {uri.Port}.");
            }
            catch (Exception e)
            {
                ret = false;
                Console.WriteLine($"[KO] Synapse server on hostname {uri.Host} could not be reached on port {uri.Port}.");   
            }
        }
        else
        {
            ret = false;
            Console.WriteLine("[KO] Synapse URI is invalid. Specify a correct URI to the Synapse server.");   
        }

        if (ret)
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.UserName = synapseSettings.UserName;
            uriBuilder.Password = synapseSettings.Password;
            try
            {
                var telepath = new TelepathClient(uriBuilder.ToString());
                using var proxy = await telepath.GetProxyAsync(TimeSpan.FromSeconds(10));
                if (proxy != null)
                {
                    Console.WriteLine(
                        $"[OK] DocIntel successfully connected to Synapse (commit: {proxy.SynCommit}, version: {string.Join(".", proxy.SynVers)})");
                }
                else
                {
                    Console.WriteLine("[KO] Synapse Telepath Proxy could not be obtained. Check that the Synapse server is up and running.");
                    ret = false;
                }

            }
            catch (SynsharpException e)
            {
                Console.WriteLine($"[KO] DocIntel could not connect to the Synapse server ({e.Message}).");
                ret = false;   
            }
        }
            
        return ret;
    }

    private static async Task<bool> EmailPreFlightChecks(EmailSettings config)
    {
        var ret = true;
        
        Console.WriteLine("---- Running pre-flight checks for emails...");

        if (config.EmailEnabled)
        {
            if (string.IsNullOrEmpty(config.SMTPServer))
            {
                ret = false;
                Console.WriteLine("[KO] SMTP URI is null or empty. Specify a correct URI to the SMTP server.");
            }
            else
            {
                Console.WriteLine($"[OK] DocIntel will use the SMTP server located at '{config.SMTPServer}:{config.SMTPPort}'.");
                Console.WriteLine($"[OK] DocIntel will log in on SMTP with '{config.SMTPUser}' username.");
            }
            
            try
            {
                var tcpClient = new TcpClient();
                tcpClient.Connect(config.SMTPServer, config.SMTPPort);
                Console.WriteLine($"[OK] SMTP server {config.SMTPServer} is reachable on port {config.SMTPPort}.");
            }
            catch (Exception e)
            {
                ret = false;
                Console.WriteLine($"[KO] SMTP server on hostname {config.SMTPServer} could not be reached on port {config.SMTPPort}.");   
            }
            
            try
            {
                using var client = new SmtpClient();
                client.CheckCertificateRevocation = config.CheckCertificateRevocation;
                await client.ConnectAsync(config.SMTPServer, config.SMTPPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(config.SMTPUser, config.SMTPPassword);
                await client.DisconnectAsync(true);
                Console.WriteLine(
                    $"[OK] DocIntel successfully connected to the SMTP Server.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[KO] DocIntel could not connect to the SMTP server ({e.Message}).");
                ret = false;   
            }
        }
        else
        {
            Console.WriteLine($"[OK] Emails are not enabled.");
        }

        return ret;
    }
}