using EasyDisk.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IFileService
    {
        Task<FileResponseDto?> UploadChunkAsync(UploadChunkDto uploadChunkDto, Stream chunkStream);
        Task<IEnumerable<FileResponseDto>> GetFilesAsync(int? folderId = null);
        Task<FileResponseDto?> UpdateFileAsync(Guid fileId, UpdateFileDto updateFileDto);
        Task SoftDeleteFileAsync(Guid fileId);
        Task HardDeleteFileAsync(Guid fileId);
    }
}
