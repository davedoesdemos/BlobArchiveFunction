# Function App to change storage object tiers by folder

**Produced by Dave Lusty**

## Introduction

This demo shows how to programmatically move data to a different tier based on a prefix using an HTTP call (REST). The video is [not ready yet](https://youtu.be/uku7HN4zaDc)

### App Settings:
storageAccountSASKey
storageAccountBaseUri

### Parameters
string targetTier - hot, cool, archive (default hot)
string objectPrefix
string containerName

## Code
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Extensions.Configuration;

namespace BlobRecall
{
    public static class BlobRecall
    {
        [FunctionName("BlobRecall")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            //get the storage account info from application settings
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var storageAccountSASKey = config["storageAccountSASKey"];
            var storageAccountBaseUri = config["storageAccountBaseUri"];

            //get the parameters from the query for tier and prefix
            string targetTier = req.Query["targetTier"];
            string objectPrefix = req.Query["objectPrefix"];
            string containerName = req.Query["containerName"];

            //set up the storage account for use
            StorageCredentials credentials = new StorageCredentials(storageAccountSASKey);
            CloudBlobClient cloudBlobClient1 = new CloudBlobClient(new StorageUri(new Uri(storageAccountBaseUri)), credentials);
            var containerobj = cloudBlobClient1.GetContainerReference(containerName);

            //create the empty response message which will be JSON
            string responseMessage = "{\n[";
            //set up the tier variable
            var tier = StandardBlobTier.Hot;

            //loop through all blobs in the container with the prefix specified
            //prefix may be a virtual folder or hierarchy
            //hierarchy will currently return an error line for the folder, this doesn't affect operation
            //true for recursive, false for not

            //!!!!
            //I don't currently filter for snapshots etc. so you may want to implement that depending on requirements
            //!!!!

            foreach (var blobItem in containerobj.ListBlobs(objectPrefix, true))
            {
                //get the blob object
                var blobobj = new CloudBlockBlob(blobItem.Uri, cloudBlobClient1);
                //set the desired tier, default is hot to avoid early deletion charges
                switch (targetTier.ToLower())
                {
                    case "cool":
                        tier = StandardBlobTier.Cool;
                        Console.WriteLine("Case 2");
                        break;
                    case "archive":
                        tier = StandardBlobTier.Archive;
                        Console.WriteLine("Case 2");
                        break;
                    default:
                        tier = StandardBlobTier.Hot;
                        Console.WriteLine("Default case");
                        break;
                }
                //set the tier and log
                try
                {
                    blobobj.SetStandardBlobTier(tier);
                    log.LogInformation(blobobj.Uri.ToString());
                    responseMessage = responseMessage + "\n  {\n  \"Object\": \"" + blobobj.Uri.ToString() + "\",\n  \"targetTier\": \"" + tier.ToString() + "\"\n  },";
                }
                //report any issues
                catch (Exception e)
                {
                    log.LogError(e.Message);
                    responseMessage = responseMessage + "\n  {\n  \"Object\": \"" + blobobj.Uri.ToString() + "\",\n  \"targetTier\": \"ERROR\"\n  },";
                }
            }
            //fix the end of the JSON output
            responseMessage = responseMessage.Remove((responseMessage.Length - 1), 1);
            responseMessage = responseMessage + "\n]\n}";
            //return the results to the client
            return new OkObjectResult(responseMessage);
        }
    }
}
```