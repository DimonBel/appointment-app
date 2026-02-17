using IdentityApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityApp.Service.Services;

public class MinioAvatarStorageService : IAvatarStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public MinioAvatarStorageService(IConfiguration configuration)
    {
        var endpoint = configuration["Minio:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["Minio:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["Minio:SecretKey"] ?? "minioadmin";
        var useSsl = bool.TryParse(configuration["Minio:UseSsl"], out var parsedSsl) && parsedSsl;

        _bucket = configuration["Minio:Bucket"] ?? "avatars";
        _publicBaseUrl = configuration["Minio:PublicBaseUrl"] ?? "http://localhost:9000";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();
    }

    public async Task<string> UploadAvatarAsync(Stream stream, long size, string fileName, string contentType, string userKey)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (size <= 0) throw new ArgumentException("Avatar file is empty.", nameof(size));

        await EnsureBucketInitializedAsync();

        var extension = GetSafeExtension(fileName, contentType);
        var sanitizedUserKey = SanitizeSegment(userKey);
        var objectName = $"{sanitizedUserKey}/{Guid.NewGuid():N}{extension}";

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(size)
            .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType));

        var encodedObjectName = string.Join('/', objectName.Split('/').Select(Uri.EscapeDataString));
        return $"{_publicBaseUrl.TrimEnd('/')}/{_bucket}/{encodedObjectName}";
    }

    private async Task EnsureBucketInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
            if (!exists)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
            }

            var policy = BuildPublicReadPolicy(_bucket);
            await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(_bucket).WithPolicy(policy));

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string BuildPublicReadPolicy(string bucket)
    {
        return $@"{{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {{
      ""Effect"": ""Allow"",
    ""Principal"": {{ ""AWS"": [""*""] }},
      ""Action"": [""s3:GetObject""],
      ""Resource"": [""arn:aws:s3:::{bucket}/*""]
    }}
  ]
}}";
    }

    private static string GetSafeExtension(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(ext) && Regex.IsMatch(ext, "^\\.[a-z0-9]{2,5}$"))
        {
            return ext;
        }

        return contentType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };
    }

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "user";

        var safe = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9._-]", "-");
        safe = Regex.Replace(safe, "-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "user" : safe;
    }
}
