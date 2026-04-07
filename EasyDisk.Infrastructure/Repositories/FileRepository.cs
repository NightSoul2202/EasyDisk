using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public FileRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(FileEntity file)
        {
            _dbContext.Files.Add(file);
            await Task.CompletedTask;
        }

        public void Delete(FileEntity file)
        {
            _dbContext.Files.Remove(file);
        }

        public async Task<List<FileEntity>> GetByFolderIdAsync(int? folderId, string ownerId)
        {
            return await _dbContext.Files.Where(f => f.FolderId == folderId && f.OwnerId == ownerId).ToListAsync();
        }

        public async Task<FileEntity?> GetByIdAsync(Guid id, string ownerId)
        {
            return await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == ownerId);
        }

        public async Task<bool> IsNameTakenAsync(string name, string extension, int? folderId, string ownerId, Guid? excludeFileId = null)
        {
            var query = _dbContext.Files
                .Where(f => f.Name == name && f.Extension == extension && f.FolderId == folderId && f.OwnerId == ownerId && f.DeletedAt == null);

            if (excludeFileId.HasValue)
            {
                query = query.Where(f => f.Id != excludeFileId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
