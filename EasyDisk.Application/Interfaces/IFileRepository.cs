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
        Task<List<FileEntity>> GetByFolderIdAsync(int? folderId, string ownerId);
        Task<bool> IsNameTakenAsync(string name, string extension, int? folderId, string ownerId, Guid? excludeFileId = null);

        Task AddAsync(FileEntity file);
        Task SaveChangesAsync();
        void Delete(FileEntity file);
    }
}
