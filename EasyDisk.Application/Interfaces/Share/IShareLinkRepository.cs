using EasyDisk.Application.DTOs;
using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Share
{
    public interface IShareLinkRepository
    {
        Task<ShareLinkEntity?> GetByTokenAsync(string token);
        Task<ShareLinkEntity?> GetByTokenWithRelationsAsync(string token);
        Task DeleteLinksForFileAsync(Guid fileId);
        Task DeleteLinksForFolderAsync(int folderId);
        Task AddAsync(ShareLinkEntity shareLink);
        Task SaveChangesAsync();
    }
}
