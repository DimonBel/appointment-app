namespace IdentityApp.Domain.Interfaces;

/// <summary>
/// Service interface for avatar image storage and management
/// Handles uploading and storing user profile avatars
/// </summary>
public interface IAvatarStorageService
{
    /// <summary>
    /// Uploads a user avatar image to storage
    /// Processes and stores the avatar image
    /// </summary>
    /// <param name="stream">Image data stream</param>
    /// <param name="size">Size of the image in bytes</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type of the image</param>
    /// <param name="userKey">Unique key identifying the user</param>
    /// <returns>URL/path to the uploaded avatar</returns>
    Task<string> UploadAvatarAsync(Stream stream, long size, string fileName, string contentType, string userKey);
}