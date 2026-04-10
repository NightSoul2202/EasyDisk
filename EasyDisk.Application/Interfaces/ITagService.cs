using EasyDisk.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface ITagService
    {
        Task<TagResponseDto> CreateTagAsync(CreateTagDto dto);
        Task<IEnumerable<TagResponseDto>> GetUserTagsAsync();
        Task AttachTagToFileAsync(Guid fileId, int tagId);
        Task DetachTagFromFileAsync(Guid fileId, int tagId);
    }
}
