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
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;

        public FolderController(IFolderService folderService)
        {
            _folderService = folderService;
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
        [Route("download/{id}")]
        public async Task<IActionResult> DownloadFolder(int id)
        {
            var (zipStream, zipName) = await _folderService.DownloadFolderAsync(id);

            return File(zipStream, "application/zip", zipName);
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
