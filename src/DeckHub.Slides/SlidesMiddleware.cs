using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Polly;
using DeckHub.Slides.Options;
using JetBrains.Annotations;

namespace DeckHub.Slides
{
    [UsedImplicitly]
    public class SlidesMiddleware
    {
        private readonly ILogger<SlidesMiddleware> _logger;
        private readonly CloudBlobClient _client;
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly Policy _putPolicy;
        private readonly Policy _getPolicy;

        // ReSharper disable once UnusedParameter.Local
        public SlidesMiddleware(RequestDelegate _, IOptions<StorageOptions> options, ILogger<SlidesMiddleware> logger, IApiKeyProvider apiKeyProvider)
        {
            _logger = logger;
            _apiKeyProvider = apiKeyProvider;
            var storageAccount = CloudStorageAccount.Parse(options.Value.ConnectionString);
            _client = storageAccount.CreateCloudBlobClient();
            _putPolicy = ResiliencePolicy.Create(logger);
            _getPolicy = ResiliencePolicy.Create(logger);
            _logger.LogWarning(nameof(SlidesMiddleware));
        }

        public Task Invoke(HttpContext context)
        {

            _logger.LogWarning("Path: {path}", context.Request.Path);
            var path = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ??
                       Array.Empty<string>();
            if (path.Length == 4)
            {
                if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    return Get(path[0], path[1], path[2], path[3], context);
                }
                if (context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    return Put(path[0], path[1], path[2], path[3], context);
                }
                if (context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    return Head(path[0], path[1], path[2], path[3], context);
                }
            }
            _logger.LogError("Path not found: {path}", context.Request.Path);
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        }

        private Task Put(string place, string presenter, string show, string index, HttpContext context)
        {
            var apiKey = context.Request.Headers["API-Key"];
            if (!_apiKeyProvider.CheckBase64(presenter, apiKey))
            {
                _logger.LogWarning(EventIds.PresenterInvalidApiKey, "Invalid API key.");
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }

            return _putPolicy.ExecuteAsync(async () =>
            {
                var directory = await GetDirectory(place, presenter, show, true);

                var blob = directory.GetBlockBlobReference($"{index}.jpg");
                blob.Properties.ContentType = context.Request.ContentType;
                await blob.UploadFromStreamAsync(context.Request.Body);
                await blob.SetPropertiesAsync();
                context.Response.StatusCode = 201;
            });
        }

        private Task Get(string place, string presenter, string show, string index, HttpContext context)
        {
            return _getPolicy.ExecuteAsync(async () =>
            {
                var directory = await GetDirectory(place, presenter, show);
                if (directory != null)
                {
                    var blob = directory.GetBlockBlobReference($"{index}.jpg");
                    if (await blob.ExistsAsync())
                    {
                        await blob.FetchAttributesAsync();
                        context.Response.Headers.ContentLength = blob.Properties.Length;
                        context.Response.Headers["Content-Type"] = blob.Properties.ContentType;
                        context.Response.Headers["ETag"] = blob.Properties.ETag;
                        context.Response.StatusCode = 200;
                        await blob.DownloadToStreamAsync(context.Response.Body);
                        return;
                    }
                }
                context.Response.StatusCode = 404;
            });
        }

        private Task Head(string place, string presenter, string show, string index, HttpContext context)
        {
            return _getPolicy.ExecuteAsync(async () =>
            {
                var directory = await GetDirectory(place, presenter, show);
                if (directory != null)
                {
                    var blob = directory.GetBlockBlobReference($"{index}.jpg");
                    if (await blob.ExistsAsync())
                    {
                        await blob.FetchAttributesAsync();
                        context.Response.Headers.ContentLength = blob.Properties.Length;
                        context.Response.Headers["Content-Type"] = blob.Properties.ContentType;
                        context.Response.Headers["ETag"] = blob.Properties.ETag;
                        context.Response.StatusCode = 200;
                        return;
                    }
                }
                context.Response.StatusCode = 404;
            });
        }

        private async Task<CloudBlobDirectory> GetDirectory(string place, string presenter, string show, bool createIfNotExist = false)
        {
            var containerRef = _client.GetContainerReference(presenter);
            if (createIfNotExist)
            {
                try
                {
                    await containerRef.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating container {container}", presenter);
                    throw;
                }
            }
            else
            {
                if (!await containerRef.ExistsAsync())
                {
                    return null;
                }
            }
            var placeDirectory = containerRef.GetDirectoryReference(place);
            return placeDirectory.GetDirectoryReference(show);
        }
    }
}