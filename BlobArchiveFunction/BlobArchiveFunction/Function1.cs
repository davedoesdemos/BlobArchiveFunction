using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace BlobArchiveFunction
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([BlobTrigger("incoming/{name}", Connection = "storageAccountConnection")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            // Azure.Storage.Blobs.Specialized.BlobBaseClient
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString();
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var container = cloudBlobClient.GetContainerReference("container");
            var blob = container.GetBlockBlobReference("blob name");
            blob.SetStandardBlobTier(StandardBlobTier.Cool);
            blob.FetchAttributes();
            var tier = blob.Properties.StandardBlobTier;
        }
    }
}
