using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IFolderRepository
    {
        Task<FolderEntity?> GetByIdAsync(int id, string ownerId);
        Task<FolderEntity?> GetByIdWithFilesAsync(int id, string ownerId);
        Task<List<FolderEntity>> GetByParentIdAsync(int? parentFolderId, string ownerId);
        Task<bool> ExistsAsync(int id, string ownerId);
        Task<bool> IsNameTakenAsync(string name, int? parentFolderId, string ownerId, int? excludeFolderId = null);

        Task AddAsync(FolderEntity folder);
        Task SaveChangesAsync();
        void Delete(FolderEntity folder);
    }
}
