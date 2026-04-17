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
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpPost]
        [Route("create-tag")]
        [Audit("Tag.Create", "Tag")]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
        {
            var tag = await _tagService.CreateTagAsync(dto);

            return Created("", tag);
        }

        [HttpPost]
        [Route("{id}/attach/{fileId}")]
        [Audit("Tag.Attach", "Tag")]
        public async Task<IActionResult> AttachTag(int id, Guid fileId)
        {
            await _tagService.AttachTagToFileAsync(fileId, id);

            return Ok(new { message = "Tag attached successfully." });
        }

        [HttpGet]
        [Route("get-user-tags")]
        public async Task<IActionResult> GetUserTags()
        {
            var tags = await _tagService.GetUserTagsAsync();

            return Ok(tags);
        }

        [HttpPut]
        [Route("{id}/update-tag")]
        [Audit("Tag.Update", "Tag")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagDto dto)
        {
            var tag = await _tagService.UpdateTagAsync(id, dto);

            return Ok(tag);
        }

        [HttpDelete]
        [Route("{id}/detach/{fileId}")]
        [Audit("Tag.Detach", "Tag")]
        public async Task<IActionResult> DetachTag(int id, Guid fileId)
        {
            await _tagService.DetachTagFromFileAsync(fileId, id);

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}/delete-tag")]
        [Audit("Tag.Delete", "Tag")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            await _tagService.DeleteTagAsync(id);

            return NoContent();
        }
    }
}
