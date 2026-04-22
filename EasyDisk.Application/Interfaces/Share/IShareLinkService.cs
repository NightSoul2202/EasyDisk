using EasyDisk.Application.DTOs;
using EasyDisk.Application.DTOs.Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Share
{
    public interface IShareLinkService
    {
        Task<ShareLinkResponseDto> CreateShareLinkAsync(CreateShareLinkDto dto);
        Task<(Stream FileStream, string ContentType, string FileName)> DownloadSharedFileAsync(string token, string? password = null);
        Task<string> PrepareSharedFolderZipAsync(string token, string? password = null);
        Task<(Stream FileStream, string ContentType, string FileName)> DownloadSharedItemAsync(string token, Guid fileId, string? password = null, int? targetFolderId = null);
        Task<IEnumerable<SharedItemDto>> GetSharedFolderContentAsync(string token, string? password = null, int? folderId = null);
        Task<ShareLinkInfoDto> GetShareLinkInfoAsync(string token);
    }
}
