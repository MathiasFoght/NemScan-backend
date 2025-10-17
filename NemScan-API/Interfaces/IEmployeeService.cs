namespace NemScan_API.Interfaces;

public interface IEmployeeService
{
    Task<string> UploadAsync(Stream stream, string blobName, string contentType);
    Task DeleteIfExistsAsync(string url);
}