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
    }
}
