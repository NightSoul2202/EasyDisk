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
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
        {
            var tag = await _tagService.CreateTagAsync(dto);

            return Created("", tag);
        }

        [HttpPost]
        [Route("{tagId}/attach/{fileId}")]
        public async Task<IActionResult> AttachTag(int tagId, Guid fileId)
        {
            await _tagService.AttachTagToFileAsync(fileId, tagId);

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
        [Route("{tagId}/update-tag")]
        public async Task<IActionResult> UpdateTag(int tagId, [FromBody] UpdateTagDto dto)
        {
            var tag = await _tagService.UpdateTagAsync(tagId, dto);

            return Ok(tag);
        }

        [HttpDelete]
        [Route("{tagId}/detach/{fileId}")]
        public async Task<IActionResult> DetachTag(int tagId, Guid fileId)
        {
            await _tagService.DetachTagFromFileAsync(fileId, tagId);

            return NoContent();
        }

        [HttpDelete]
        [Route("{tagId}/delete-tag")]
        public async Task<IActionResult> DeleteTag(int tagId)
        {
            await _tagService.DeleteTagAsync(tagId);

            return NoContent();
        }
    }
}
