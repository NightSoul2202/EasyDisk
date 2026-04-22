using EasyDisk.Application.DTOs;
using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IFileRepository
    {
        Task<FileEntity?> GetByIdAsync(Guid id, string ownerId);
        Task<FileEntity?> GetByIdWithTagsAsync(Guid id, string ownerId);
        Task<List<FileEntity>> GetByFolderIdAsync(int? folderId, string ownerId);
        Task<FileEntity?> GetByNameWithVersionsAsync(string name, string extension, int? folderId, string ownerId);
        Task<FileEntity?> GetByIdWithVersionsAsync(Guid id, string ownerId);
        Task<bool> IsNameTakenAsync(string name, string extension, int? folderId, string ownerId, Guid? excludeFileId = null);
        Task<List<FileEntity>> SearchFilesAsync(FileSearchParametersDto searchTerm, string ownerId);
        Task<List<FileEntity>> GetDeletedFilesAsync(string userId);
        Task<FileEntity?> GetDeletedFileByIdAsync(Guid id, string userId);
        Task UpdateAsync(FileEntity file);

        Task AddFileVersionAsync(FileVersionEntity version);
        Task AddAsync(FileEntity file);
        Task SaveChangesAsync();
        void Delete(FileEntity file);
    }
}
