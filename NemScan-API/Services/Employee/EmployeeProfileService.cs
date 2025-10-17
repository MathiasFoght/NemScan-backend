using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NemScan_API.Interfaces;

namespace NemScan_API.Services.Employee;

public class EmployeeProfileService : IEmployeeService
{
    private readonly BlobContainerClient _container;

    public EmployeeProfileService(IConfiguration config)
    {
        var connectionString = config["AZURE_STORAGE_CONNECTION"];
        var containerName = config["AZURE_STORAGE_CONTAINER_NAME"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
            throw new InvalidOperationException("Azure Storage config missing.");

        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> UploadAsync(Stream stream, string blobName, string contentType)
    {
        var client = _container.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await client.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = headers,
                TransferOptions = new StorageTransferOptions { MaximumConcurrency = 2 }
            }
        );

        return client.Uri.ToString();
    }
    
    public async Task DeleteIfExistsAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var baseUri = _container.Uri.ToString().TrimEnd('/') + "/";
        if (!url.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase)) return;

        var blobName = url.Substring(baseUri.Length);
        await _container.DeleteBlobIfExistsAsync(blobName);
    }

}