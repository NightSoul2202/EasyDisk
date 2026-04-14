using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        public async Task<FileEntity?> GetByIdWithTagsAsync(Guid id, string ownerId)
        {
            return await _dbContext.Files.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == ownerId);
        }

        public async Task<FileEntity?> GetByNameWithVersionsAsync(string name, string extension, int? folderId, string ownerId)
        {
            return await _dbContext.Files
                .Include(f => f.Versions)
                .FirstOrDefaultAsync(f => f.Name == name && f.Extension == extension && f.FolderId == folderId && f.OwnerId == ownerId);
        }

        public async Task<FileEntity?> GetByIdWithVersionsAsync(Guid id, string ownerId)
        {
            return await _dbContext.Files.Include(f => f.Versions).FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == ownerId);
        }

        public async Task<bool> IsNameTakenAsync(string name, string extension, int? folderId, string ownerId, Guid? excludeFileId = null)
        {
            var query = _dbContext.Files
                .Where(f => f.Name == name && f.Extension == extension && f.FolderId == folderId && f.OwnerId == ownerId);

            if (excludeFileId.HasValue)
            {
                query = query.Where(f => f.Id != excludeFileId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<FileEntity>> SearchFilesAsync(FileSearchParametersDto searchTerm, string ownerId)
        {
            var query = _dbContext.Files
                .Include(f => f.Tags)
                .Where(f => f.OwnerId == ownerId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm.SearchTerm))
            {
                query = query.Where(f => f.Name.Contains(searchTerm.SearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm.Extension))
            {
                query = query.Where(f => f.Extension == searchTerm.Extension);
            }

            if (searchTerm.FolderId.HasValue)
            {
                query = query.Where(f => f.FolderId == searchTerm.FolderId);
            }

            if (searchTerm.TagIds != null && searchTerm.TagIds.Any())
            {
                foreach (var tagId in searchTerm.TagIds)
                {
                    query = query.Where(f => f.Tags.Any(t => t.Id == tagId));
                }
            }

            return await query.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
