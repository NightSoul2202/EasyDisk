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

        [HttpPut]
        [Route("update-file/{fileId}")]
        public async Task<IActionResult> UpdateFile(Guid fileId, [FromBody] UpdateFileDto updateFileDto)
        {
            var result = await _fileService.UpdateFileAsync(fileId, updateFileDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("cancel-upload/{uploadId}")]
        public async Task<IActionResult> CancelUpload(string uploadId)
        {
            await _fileService.CancelUploadAsync(uploadId);
            return NoContent();
        }

        [HttpDelete]
        [Route("soft-delete-file/{fileId}")]
        public async Task<IActionResult> SoftDeleteFile(Guid fileId)
        {
            await _fileService.SoftDeleteFileAsync(fileId);
            return NoContent();
        }

        [HttpDelete]
        [Route("hard-delete-file/{fileId}")]
        public async Task<IActionResult> HardDeleteFile(Guid fileId)
        {
            await _fileService.HardDeleteFileAsync(fileId);
            return NoContent();
        }
    }
}
