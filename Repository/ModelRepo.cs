using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Azure;
using Azure.Storage.Blobs.Models;

namespace Repository;
public class ModelRepo
{
    public async Task UploadMetricVersion(string metricPath)
    {
        System.Console.WriteLine("uploading saved metrics to blob store...");
        string metricContainerName = "modelrepositoryprod-metrics";
        string metricBlobName = "sentimentanalysismodel-metrics";
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=modelrepositoryprod;AccountKey=/oRe/ytUUWwonN3kZ9YtPytHKCav+SizB6516u/6We9yxOU9cz3Z7ZforJE6OUssYMW4eQItB09h+AStk38oVA==;EndpointSuffix=core.windows.net";

        BlobServiceClient metricBlobServiceClient = new BlobServiceClient(connectionString);
        BlobContainerClient metricContainerClient = metricBlobServiceClient.GetBlobContainerClient(metricContainerName);

        using (FileStream zipStream = new FileStream(metricPath, System.IO.FileMode.Open))
        {
            BlobClient blobClient = metricContainerClient.GetBlobClient(metricBlobName);
            await blobClient.UploadAsync(metricPath, true);
            System.Console.WriteLine("uploaded model metrics");
            // Update the blob's metadata to trigger the creation of a new version.
            Dictionary<string, string> metadata = new Dictionary<string, string>
            {
                { "author", "PradeepLoganathan" },
                { "datetime", DateTime.UtcNow.ToLongDateString() }
            };

            Response<BlobInfo> metadataResponse = 
                await blobClient.SetMetadataAsync(metadata);

            // Get the version ID for the new current version.
            string newVersionId = metadataResponse.Value.VersionId;
            System.Console.WriteLine("Updated model metrics version");
        }

    }
    public async Task UploadModelVersion(string modelPath)
    {
        System.Console.WriteLine("uploading saved model to blob store...");
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=modelrepositoryprod;AccountKey=/oRe/ytUUWwonN3kZ9YtPytHKCav+SizB6516u/6We9yxOU9cz3Z7ZforJE6OUssYMW4eQItB09h+AStk38oVA==;EndpointSuffix=core.windows.net";
        string modelContainerName = "modelrepositoryprod";
        string modelBlobName = "sentimentanalysismodel";

        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(modelContainerName);

        using (FileStream zipStream = new FileStream(modelPath, System.IO.FileMode.Open))
        {
            BlobClient blobClient = containerClient.GetBlobClient(modelBlobName);
            await blobClient.UploadAsync(zipStream, true);
            System.Console.WriteLine("Uplaoded model to azure blob store");
             // Update the blob's metadata to trigger the creation of a new version.
            Dictionary<string, string> metadata = new Dictionary<string, string>
            {
                { "author", "PradeepLoganathan" },
                { "datetime", DateTime.UtcNow.ToLongDateString() }
            };

            Response<BlobInfo> metadataResponse = 
                await blobClient.SetMetadataAsync(metadata);

            // Get the version ID for the new current version.
            string newVersionId = metadataResponse.Value.VersionId;
            System.Console.WriteLine("Upload Complete ....Uploaded trained model version");
        }
    }
}