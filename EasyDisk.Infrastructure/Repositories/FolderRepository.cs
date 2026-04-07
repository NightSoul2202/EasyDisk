using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public FolderRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(FolderEntity folder)
        {
            _dbContext.Folders.Add(folder);
            await Task.CompletedTask;
        }

        public void Delete(FolderEntity folder)
        {
            _dbContext.Folders.Remove(folder);
        }

        public async Task<bool> ExistsAsync(int id, string ownerId)
        {
            return await _dbContext.Folders.AnyAsync(f => f.Id == id && f.OwnerId == ownerId);
        }

        public async Task<FolderEntity?> GetByIdAsync(int id, string ownerId)
        {
            return await _dbContext.Folders
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == ownerId);
        }

        public async Task<FolderEntity?> GetByIdWithFilesAsync(int id, string ownerId)
        {
            return await _dbContext.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == ownerId);
        }

        public async Task<List<FolderEntity>> GetByParentIdAsync(int? parentFolderId, string ownerId)
        {
            return await _dbContext.Folders
                .Where(f => f.ParentFolderId == parentFolderId && f.OwnerId == ownerId)
                .ToListAsync();
        }

        public async Task<bool> IsNameTakenAsync(string name, int? parentFolderId, string ownerId, int? excludeFolderId = null)
        {
            var query = _dbContext.Folders
                .Where(f => f.Name == name && f.ParentFolderId == parentFolderId && f.OwnerId == ownerId);

            if(excludeFolderId.HasValue)
            {
                query = query.Where(f => f.Id != excludeFolderId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
