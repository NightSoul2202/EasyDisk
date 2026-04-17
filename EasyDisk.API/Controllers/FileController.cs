using EasyDisk.API.Filters;
using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost]
        [Route("upload-chunk")]
        [Audit("File.Upload", "File")]
        public async Task<IActionResult> UploadChunk([FromForm] UploadChunkDto uploadChunkDto, IFormFile chunk)
        {
            if (chunk == null || chunk.Length == 0)
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

        [HttpGet]
        [Route("download/{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var (fileStream, contentType, fileName) = await _fileService.DownloadFileAsync(id);

            return File(fileStream, contentType, fileName);
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
