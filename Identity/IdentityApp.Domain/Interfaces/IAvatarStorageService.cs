namespace IdentityApp.Domain.Interfaces;

public interface IAvatarStorageService
{
    Task<string> UploadAvatarAsync(Stream stream, long size, string fileName, string contentType, string userKey);
}
