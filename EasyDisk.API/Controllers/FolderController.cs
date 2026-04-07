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

        [HttpPut]
        [Route("update-folder/{folderId}")]
        public async Task<IActionResult> UpdateFolder(int folderId, [FromBody] UpdateFolderDto updateFolderDto)
        {
            var result = await _folderService.UpdateFolderAsync(folderId, updateFolderDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("soft-delete-folder/{folderId}")]
        public async Task<IActionResult> SoftDeleteFolder(int folderId)
        {
            await _folderService.SoftDeleteFolderAsync(folderId);
            return NoContent();
        }

        [HttpDelete]
        [Route("hard-delete-folder/{folderId}")]
        public async Task<IActionResult> HardDeleteFolder(int folderId)
        {
            await _folderService.HardDeleteFolderAsync(folderId);
            return NoContent();
        }
    }
}
