using EasyDisk.API.Filters;
using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace EasyDisk.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly IMemoryCache _cache;

        public FolderController(IFolderService folderService, IMemoryCache cache)
        {
            _folderService = folderService;
            _cache = cache;
        }

        [HttpPost]
        [Route("create-folder")]
        [Audit("Folder.Create", "Folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto createFolderDto)
        {
            var result = await _folderService.CreateFolderAsync(createFolderDto);

            return Created("", result);
        }

        [HttpGet]
        [Route("get-folders")]
        public async Task<IActionResult> GetFolders([FromQuery] int? parentFolderId = null)
        {
            var result = await _folderService.GetFoldersAsync(parentFolderId);

            return Ok(result);
        }

        [HttpPost]
        [Route("prepare-zip/{id}")]
        public async Task<IActionResult> PrepareZip(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tempFileName = await _folderService.PrepareZipTaskAsync(id, userId!);

            return Ok(new { tempFileName });
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("download-temp/{fileName}")]
        public IActionResult DownloadTempZip(string fileName)
        {
            string fullPath = Path.Combine(Path.GetTempPath(), fileName);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(fileStream, "application/zip", "folder.zip");
        }

        [HttpGet]
        [Route("{id}/path")]
        public async Task<IActionResult> GetFolderPath(int id)
        {
            var path = await _folderService.GetFolderPathAsync(id);

            return Ok(path);
        }

        [HttpPut]
        [Route("{id}/move")]
        [Audit("Folder.Move", "Folder")]
        public async Task<IActionResult> MoveFolder(int id, [FromBody] MoveItemDto dto)
        {
            await _folderService.MoveFolderAsync(id, dto.TargetFolderId);

            return NoContent();
        }

        [HttpPut]
        [Route("update-folder/{id}")]
        [Audit("Folder.Update", "Folder")]
        public async Task<IActionResult> UpdateFolder(int id, [FromBody] UpdateFolderDto updateFolderDto)
        {
            var result = await _folderService.UpdateFolderAsync(id, updateFolderDto);

            return Ok(result);
        }

        [HttpDelete]
        [Route("soft-delete-folder/{id}")]
        [Audit("Folder.SoftDelete", "Folder")]
        public async Task<IActionResult> SoftDeleteFolder(int id)
        {
            await _folderService.SoftDeleteFolderAsync(id);

            return NoContent();
        }

        [HttpDelete]
        [Route("hard-delete-folder/{id}")]
        [Audit("Folder.HardDelete", "Folder")]
        public async Task<IActionResult> HardDeleteFolder(int id)
        {
            await _folderService.HardDeleteFolderAsync(id);

            return NoContent();
        }
    }
}
