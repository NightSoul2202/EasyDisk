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

        [HttpGet]
        [Route("{id}/download-ticket")]
        public IActionResult GetDownloadTicket(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var ticket = Guid.NewGuid().ToString("N");

            _cache.Set($"FolderTicket_{id}_{ticket}", userId, TimeSpan.FromSeconds(60));

            return Ok(new { Ticket = ticket });
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("download/{id}")]
        public async Task<IActionResult> DownloadFolder(int id, [FromQuery] string ticket)
        {
            var cacheKey = $"FolderTicket_{id}_{ticket}";
            if (string.IsNullOrEmpty(ticket) || !_cache.TryGetValue(cacheKey, out string? userId))
            {
                return Unauthorized("Invalid or expired download ticket.");
            }

            _cache.Remove(cacheKey);

            var (zipStream, zipName) = await _folderService.DownloadFolderAsync(id, userId!);

            return File(zipStream, "application/zip", zipName);
        }

        [HttpGet]
        [Route("{id}/path")]
        public async Task<IActionResult> GetFolderPath(int id)
        {
            var path = await _folderService.GetFolderPathAsync(id);

            return Ok(path);
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
