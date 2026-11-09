using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>Media uploads for products. Staff only.</summary>
    [Route("api/upload")]
    [Authorize(Policy = AuthPolicies.Staff)]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public class UploadController : ApiControllerBase
    {
        // Request limits sit slightly above the file limits to leave room for multipart overhead.
        // The exact per-file limit is enforced by FileStorageService from configuration.
        private const long ImageRequestLimit = 6 * 1024 * 1024;
        private const long VideoRequestLimit = 52 * 1024 * 1024;

        private readonly FileStorageService _storage;

        public UploadController(FileStorageService storage)
        {
            _storage = storage;
        }

        [HttpPost("image")]
        [RequestSizeLimit(ImageRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = ImageRequestLimit)]
        public Task<ActionResult<UploadResult>> UploadImage(IFormFile? file, CancellationToken ct) =>
            Save(file, MediaKind.Image, ct);

        [HttpPost("video")]
        [RequestSizeLimit(VideoRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = VideoRequestLimit)]
        public Task<ActionResult<UploadResult>> UploadVideo(IFormFile? file, CancellationToken ct) =>
            Save(file, MediaKind.Video, ct);

        private async Task<ActionResult<UploadResult>> Save(IFormFile? file, MediaKind kind, CancellationToken ct)
        {
            var result = await _storage.SaveAsync(file, kind, ct);
            if (!result.Success)
                return ValidationError("file", result.Error!);

            return new UploadResult { Url = result.Url };
        }
    }
}
