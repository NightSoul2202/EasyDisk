using EasyDisk.API.Filters;
using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController : ControllerBase
    {
        private readonly IShareLinkService _shareLinkService;

        public ShareController(IShareLinkService shareLinkService)
        {
            _shareLinkService = shareLinkService;
        }

        [Authorize]
        [HttpPost]
        [Route("create-link")]
        [Audit("ShareLink.Create", "Link")]
        public async Task<IActionResult> CreateShareLink([FromBody] CreateShareLinkDto dto)
        {
            var shareLink = await _shareLinkService.CreateShareLinkAsync(dto);

            return Ok(shareLink);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("folder-content/{token}")]
        public async Task<IActionResult> GetSharedFolderContent(string token, [FromQuery] int? folderId, [FromBody] VerifyPasswordDto dto)
        {
            var result = await _shareLinkService.GetSharedFolderContentAsync(token, dto.Password, folderId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("s/{token}")]
        public async Task<IActionResult> DownloadPublic(string token, [FromQuery] string? password = null)
        {
            var (fileStream, contentType, fileName) = await _shareLinkService.DownloadByTokenAsync(token, password);

            return File(fileStream, contentType, fileName);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("s/{token}/item/{fileId}")]
        public async Task<IActionResult> DownloadSharedItem(string token, Guid fileId, [FromQuery] string? password = null, [FromQuery] int? folderId = null)
        {
            var (fileStream, contentType, fileName) = await _shareLinkService.DownloadSharedItemAsync(token, fileId, password, folderId);

            return File(fileStream, contentType, fileName);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("info/{token}")]
        public async Task<IActionResult> GetShareLinkInfo(string token)
        {
            var result = await _shareLinkService.GetShareLinkInfoAsync(token);

            return Ok(result);
        }
    }
}
