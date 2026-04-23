using EasyDisk.API.Filters;
using EasyDisk.Application.DTOs.Files;
using EasyDisk.Application.Interfaces.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace EasyDisk.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IMemoryCache _cache;

        public FileController(IFileService fileService, IMemoryCache cache)
        {
            _fileService = fileService;
            _cache = cache;
        }

        [HttpPost]
        [Route("upload-chunk")]
        [Audit("File.Upload", "File")]
        public async Task<IActionResult> UploadChunk([FromForm] UploadChunkDto uploadChunkDto, IFormFile chunk)
        {
            if (chunk == null)
            {
                return BadRequest(new { message = "Chunk file is required." });
            }

            using var stream = chunk.OpenReadStream();

            var result = await _fileService.UploadChunkAsync(uploadChunkDto, stream);
            if (result == null)
            {
                return Created("", result);
            }

            return Ok(new { message = $"Chunk {uploadChunkDto.ChunkIndex + 1} from {uploadChunkDto.TotalChunks} successful uploaded." });
        }

        [HttpPost]
        [Route("{id}/versions/{versionNumber}/restore")]
        [Audit("File.RestoreVersion", "File")]
        public async Task<IActionResult> RestoreFileVersion(Guid id, int versionNumber)
        {
            await _fileService.RestoreFileVersionAsync(id, versionNumber);

            return Ok();
        }

        [HttpGet]
        [Route("{id}/download-ticket")]
        public IActionResult GetDownloadTicket(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var ticket = Guid.NewGuid().ToString("N");

            _cache.Set($"FileTicket_{id}_{ticket}", userId, TimeSpan.FromSeconds(60));

            return Ok(new { Ticket = ticket });
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("download/{id}")]
        public async Task<IActionResult> DownloadFile(Guid id, [FromQuery] string ticket)
        {
            var cacheKey = $"FileTicket_{id}_{ticket}";
            if (string.IsNullOrEmpty(ticket) || !_cache.TryGetValue(cacheKey, out string? userId))
            {
                return Unauthorized("Invalid or expired download ticket.");
            }

            _cache.Remove(cacheKey);

            var result = await _fileService.DownloadFileAsync(id, userId!);

            return File(result.FileStream, result.ContentType, result.FileName);
        }

        [HttpGet]
        [Route("get-files")]
        public async Task<IActionResult> GetFiles([FromQuery] int? folderId = null)
        {
            var files = await _fileService.GetFilesAsync(folderId);

            return Ok(files);
        }

        [HttpGet]
        [Route("{id}/versions")]
        public async Task<IActionResult> GetFileVersions(Guid id)
        {
            var versions = await _fileService.GetFileVersionsAsync(id);

            return Ok(versions);
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> SearchFiles([FromQuery] FileSearchParametersDto dto)
        {
            var files = await _fileService.SearchFilesAsync(dto);

            return Ok(files);
        }

        [HttpGet]
        [Route("{id}/preview")]
        public async Task<IActionResult> PreviewFile(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var (fileStream, contentType, fileName) = await _fileService.DownloadFileAsync(id, userId);

            return File(fileStream, contentType, enableRangeProcessing: true);
        }

        [HttpPut]
        [Route("{id}/move")]
        [Audit("File.Move", "File")]
        public async Task<IActionResult> MoveFile(Guid id, [FromBody] MoveItemDto dto)
        {
            await _fileService.MoveFileAsync(id, dto.TargetFolderId);

            return NoContent();
        }

        [HttpPut]
        [Route("update-file/{id}")]
        [Audit("File.Update", "File")]
        public async Task<IActionResult> UpdateFile(Guid id, [FromBody] UpdateFileDto updateFileDto)
        {
            var result = await _fileService.UpdateFileAsync(id, updateFileDto);

            return Ok(result);
        }

        [HttpDelete]
        [Route("cancel-upload/{id}")]
        [Audit("File.CancelUpload", "File")]
        public async Task<IActionResult> CancelUpload(string id)
        {
            await _fileService.CancelUploadAsync(id);

            return NoContent();
        }

        [HttpDelete]
        [Route("soft-delete-file/{id}")]
        [Audit("File.SoftDelete", "File")]
        public async Task<IActionResult> SoftDeleteFile(Guid id)
        {
            await _fileService.SoftDeleteFileAsync(id);

            return NoContent();
        }

        [HttpDelete]
        [Route("hard-delete-file/{id}")]
        [Audit("File.HardDelete", "File")]
        public async Task<IActionResult> HardDeleteFile(Guid id)
        {
            await _fileService.HardDeleteFileAsync(id);

            return NoContent();
        }
    }
}
