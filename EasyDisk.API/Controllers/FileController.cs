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
    }
}
