using Amazon.S3;
using Amazon.S3.Model;
using System.Net;

namespace Roblox.Services;

public class R2StorageService : ServiceBase, IService
{
    private readonly AmazonS3Client _client;

    public R2StorageService()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{Configuration.R2AccountId}.r2.cloudflarestorage.com",
        };
        _client = new AmazonS3Client(Configuration.R2AccessKey, Configuration.R2SecretKey, config);
    }

    public string GetPrefixFromLocalDirectory(string? directory)
    {
        if (directory == Configuration.ThumbnailsDirectory) return "images/thumbnails/";
        if (directory == Configuration.GroupIconsDirectory) return "images/groups/";
        return "assets/";
    }

    public async Task UploadFileAsync(string key, Stream content, string contentType = "application/octet-stream")
    {
        var request = new PutObjectRequest
        {
            BucketName = Configuration.R2BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true
        };
        await _client.PutObjectAsync(request);
    }

    public async Task<Stream?> GetFileAsync(string key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = Configuration.R2BucketName,
                Key = key
            };
            var response = await _client.GetObjectAsync(request);
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteFileAsync(string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = Configuration.R2BucketName,
                Key = key
            };
            await _client.DeleteObjectAsync(request);
        }
        catch (AmazonS3Exception)
        {
            // if missing, say bismillah and ignore.
        }
    }
    

    public async Task<bool> FileExistsAsync(string key)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = Configuration.R2BucketName,
                Key = key
            };
            await _client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public string GetPresignedUrl(string key, TimeSpan expiresIn)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Configuration.R2BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET
        };
        return _client.GetPreSignedURL(request);
    }
    
    public static string GetPublicUrl(string key)
    {
        return $"{Configuration.CdnBaseUrl.Trim('/')}/{key}";
    }
    
    public bool IsThreadSafe() => true;
    public bool IsReusable() => true;
}