using EasyDisk.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IFolderService
    {
        Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto createFolderDto);
        Task<(Stream ZipStream, string ZipName)> DownloadFolderAsync(int folderId);
        Task<IEnumerable<FolderResponseDto>> GetFoldersAsync(int? parentFolderId = null);
        Task<IEnumerable<FolderResponseDto>> GetFolderPathAsync(int folderId);
        Task<FolderResponseDto> UpdateFolderAsync(int folderId, UpdateFolderDto updateFolderDto);
        Task SoftDeleteFolderAsync(int folderId);
        Task HardDeleteFolderAsync(int folderId);
    }
}
