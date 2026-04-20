using EasyDisk.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IShareLinkService
    {
        Task<ShareLinkResponseDto> CreateShareLinkAsync(CreateShareLinkDto dto);
        Task<ShareLinkInfoDto> GetShareLinkInfoAsync(string token);
        Task<(Stream FileStream, string ContentType, string FileName)> DownloadByTokenAsync(string token, string? password = null);
    }
}
