using EasyDisk.Application.DTOs;
using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IShareLinkRepository
    {
        Task<ShareLinkEntity?> GetByTokenAsync(string token);
        Task AddAsync(ShareLinkEntity shareLink);
        Task SaveChangesAsync();
    }
}
