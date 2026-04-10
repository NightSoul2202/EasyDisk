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
        public async Task<IActionResult> CreateShareLink([FromBody] CreateShareLinkDto dto)
        {
            var shareLink = await _shareLinkService.CreateShareLinkAsync(dto);
            return Ok(shareLink);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("s/{token}")]
        public async Task<IActionResult> DownloadPublic(string token, [FromQuery] string? password = null)
        {
            var (fileStream, contentType, fileName) = await _shareLinkService.DownloadByTokenAsync(token, password);
            return File(fileStream, contentType, fileName);
        }
    }
}
