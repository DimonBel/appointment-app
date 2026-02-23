using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace DocumentApp.Service.Services;

public interface IMinioDocumentStorageService
{
    Task EnsureBucketExistsAsync(string bucketName);
    Task<string> UploadFileAsync(Stream fileStream, long fileSize, string objectName, string bucketName, string contentType);
    Task<Stream> DownloadFileAsync(string objectName, string bucketName);
    Task<bool> DeleteFileAsync(string objectName, string bucketName);
    Task<string> GetPresignedUrlAsync(string objectName, string bucketName, int expiresInMinutes = 60);
}

public class MinioDocumentStorageService : IMinioDocumentStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioDocumentStorageService> _logger;

    public MinioDocumentStorageService(
        string endpoint,
        string accessKey,
        string secretKey,
        bool useSSL = false,
        ILogger<MinioDocumentStorageService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MinioDocumentStorageService>.Instance;

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            var args = new BucketExistsArgs().WithBucket(bucketName);
            var exists = await _minioClient.BucketExistsAsync(args);

            if (!exists)
            {
                var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeArgs);
                _logger.LogInformation("Created bucket: {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", bucketName);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, long fileSize, string objectName, string bucketName, string contentType)
    {
        try
        {
            await EnsureBucketExistsAsync(bucketName);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileSize)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.LogInformation("Uploaded file: {ObjectName} to bucket: {BucketName}", objectName, bucketName);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {ObjectName}", objectName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string objectName, string bucketName)
    {
        try
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {ObjectName}", objectName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string objectName, string bucketName)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            _logger.LogInformation("Deleted file: {ObjectName} from bucket: {BucketName}", objectName, bucketName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {ObjectName}", objectName);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string objectName, string bucketName, int expiresInMinutes = 60)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiresInMinutes * 60);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for: {ObjectName}", objectName);
            throw;
        }
    }
}