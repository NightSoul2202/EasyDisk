using EasyDisk.API.Filters;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrashController : ControllerBase
    {
        private readonly ITrashService _trashService;

        public TrashController(ITrashService trashService)
        {
            _trashService = trashService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrashItems()
        {
            var items = await _trashService.GetTrashItemsAsync();

            return Ok(items);
        }

        [HttpPost]
        [Route("restore/{id}")]
        [Audit("Trash.Restore", "Mixed")]
        public async Task<IActionResult> RestoreItem(string id, [FromQuery] bool isFolder)
        {
            await _trashService.RestoreItemAsync(id, isFolder);

            return Ok( new { message = "Item restored successfully." });
        }

        [HttpDelete]
        [Route("empty")]
        [Audit("Trash.Empty", "Mixed")]
        public async Task<IActionResult> EmptyTrash()
        {
            await _trashService.EmptyTrashAsync();

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        [Audit("Trash.HardDelete", "Mixed")]
        public async Task<IActionResult> HardDeleteItem(string id, [FromQuery] bool isFolder)
        {
            await _trashService.HardDeleteItemAsync(id, isFolder);

            return NoContent();
        }
    }
}
