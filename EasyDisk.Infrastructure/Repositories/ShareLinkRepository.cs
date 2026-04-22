using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces.Share;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyDisk.Infrastructure.Repositories
{
    public class ShareLinkRepository : IShareLinkRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ShareLinkRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ShareLinkEntity?> GetByTokenAsync(string token)
        {
            return await _dbContext.ShareLinks
                .Include(s => s.File)
                .FirstOrDefaultAsync(s => s.Token == token);
        }

        public async Task<ShareLinkEntity?> GetByTokenWithRelationsAsync(string token)
        {
            return await _dbContext.ShareLinks
                .Include(s => s.File)
                .Include(s => s.Folder)
                .FirstOrDefaultAsync(s => s.Token == token);
        }

        public async Task DeleteLinksForFileAsync(Guid fileId)
        {
            var links = await _dbContext.ShareLinks.Where(l => l.FileId == fileId).ToListAsync();
            if (links.Any())
            {
                _dbContext.ShareLinks.RemoveRange(links);
            }
        }

        public async Task DeleteLinksForFolderAsync(int folderId)
        {
            var links = await _dbContext.ShareLinks.Where(l => l.FolderId == folderId).ToListAsync();
            if (links.Any())
            {
                _dbContext.ShareLinks.RemoveRange(links);
            }
        }

        public Task AddAsync(ShareLinkEntity shareLink)
        {
            _dbContext.ShareLinks.Add(shareLink);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
