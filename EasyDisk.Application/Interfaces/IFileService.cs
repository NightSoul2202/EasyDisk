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
        Task<(Stream FileStream, string ContentType, string FileName)> DownloadFileAsync(Guid fileId);
        Task CancelUploadAsync(string uploadId);
        Task<IEnumerable<FileResponseDto>> GetFilesAsync(int? folderId = null);
        Task<IEnumerable<FileResponseDto>> SearchFilesAsync(FileSearchParametersDto dto);
        Task<FileResponseDto?> UpdateFileAsync(Guid fileId, UpdateFileDto updateFileDto);
        Task SoftDeleteFileAsync(Guid fileId);
        Task HardDeleteFileAsync(Guid fileId);
    }
}
