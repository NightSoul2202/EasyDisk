using EasyDisk.Application.DTOs;
using EasyDisk.Application.DTOs.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Files
{
    public interface IFolderService
    {
        Task<FolderResponseDto> CreateFolderAsync(CreateFolderDto createFolderDto);
        Task<string> PrepareZipTaskAsync(int folderId, string userId);
        Task MoveFolderAsync(int folderId, int? targetParentId);
        Task<IEnumerable<FolderResponseDto>> GetFoldersAsync(int? parentFolderId = null);
        Task<IEnumerable<FolderResponseDto>> GetFolderPathAsync(int folderId);
        Task<FolderResponseDto> UpdateFolderAsync(int folderId, UpdateFolderDto updateFolderDto);
        Task RestoreFolderAsync(int folderId);
        Task SoftDeleteFolderAsync(int folderId);
        Task HardDeleteFolderAsync(int folderId);
    }
}
