using System; 
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace BlobArchiveFunction
{

    public static class Function1
    {
        [FunctionName("ArchiveBlobs")]
        public static void EventGridTest([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log, ExecutionContext context)
        {
            //get the storage account info from application settings
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var blobAccount = config["blobAccount"];

            //get the data from the event trigger and deserialise the JSON
            dynamic dataObject = JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

            //get the whole url
            string url = dataObject.url;

            //find and extract the object path from the url
            Regex regex = new Regex(@"blob.core.windows.net\/(.*)");
            Match match = regex.Match(url);
            if (match.Success)
            {
                string path = match.Groups[1].Value;
                log.LogInformation("Path: " + path);
                //find the blob details for the container and object
                //NOTE: These can be filtered here should you need to restrict archival to one container

                //container name
                string container = path.Substring(0, path.IndexOf('/'));
                log.LogInformation("Container: " + container);

                //object path including hierarchy for ADLS Gen2
                string blob = path.Substring(path.IndexOf('/'), (path.Length - path.IndexOf('/')));
                log.LogInformation("Blob: " + blob);

                //Connect to cloud storage and set the object to archive tier
                var cloudBlobClient = new CloudBlobClient(new System.Uri(blobAccount));
                var containerobj = cloudBlobClient.GetContainerReference(container);
                var blobobj = containerobj.GetBlockBlobReference(blob);
                try 
                {
                    blobobj.SetStandardBlobTier(StandardBlobTier.Archive);
                    log.LogInformation("Archived " + blob);
                }
                catch (Exception e)
                {
                    log.LogError("Error: " + e.Message);
                }
            }
        }
    }
}
