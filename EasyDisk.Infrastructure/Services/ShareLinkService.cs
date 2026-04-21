using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Repositories;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class ShareLinkService : IShareLinkService
    {
        private readonly IShareLinkRepository _shareLinkRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public ShareLinkService(
            IShareLinkRepository shareLinkRepository,
            IFileRepository fileRepository,
            IFolderRepository folderRepository,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService)
        {
            _shareLinkRepository = shareLinkRepository;
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
        }
        
        public async Task<ShareLinkResponseDto> CreateShareLinkAsync(CreateShareLinkDto dto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to create a share link.");

            if (dto.FileId == null && dto.FolderId == null)
                throw new ValidationException("You must provide either a FileId or a FolderId.");

            if (dto.FileId != null && dto.FolderId != null)
                throw new ValidationException("Cannot share both a file and a folder in a single link.");

            var token = Guid.NewGuid().ToString("N").Substring(0, 12);

            var shareLink = new ShareLinkEntity
            {
                Id = Guid.NewGuid(),
                Token = token,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = dto.ExpirationHours.HasValue
                    ? DateTime.UtcNow.AddHours(dto.ExpirationHours.Value)
                    : null
            };

            if (!string.IsNullOrEmpty(dto.Password))
            {
                shareLink.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            if (dto.FileId.HasValue)
            {
                var file = await _fileRepository.GetByIdAsync(dto.FileId.Value, userId)
                    ?? throw new NotFoundException("File", dto.FileId);

                shareLink.FileId = dto.FileId;
            }
            else if (dto.FolderId.HasValue)
            {
                var folder = await _folderRepository.GetByIdAsync(dto.FolderId.Value, userId)
                    ?? throw new NotFoundException("Folder", dto.FolderId.Value);

                shareLink.FolderId = dto.FolderId.Value;
            }

            await _shareLinkRepository.AddAsync(shareLink);
            await _shareLinkRepository.SaveChangesAsync();

            return new ShareLinkResponseDto
            {
                Token = token,
                DownloadUrl = $"/api/Share/s/{token}",
                ExpirationDate = shareLink.ExpirationDate,
                IsPasswordProtected = !string.IsNullOrEmpty(shareLink.PasswordHash)
            };
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadByTokenAsync(string token, string? password = null)
        {
            var shareLink = await _shareLinkRepository.GetByTokenWithRelationsAsync(token);

            if (shareLink == null || (shareLink.ExpirationDate.HasValue && shareLink.ExpirationDate.Value < DateTime.UtcNow))
            {
                throw new NotFoundException("Share link", token);
            }

            if (!string.IsNullOrEmpty(shareLink.PasswordHash))
            {
                if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, shareLink.PasswordHash))
                {
                    throw new ValidationException("Invalid password for share link.");
                }
            }

            if (shareLink.FileId.HasValue)
            {
                var stream = await _fileStorageService.GetFileStreamAsync(shareLink.File!.PhysicalPath);

                shareLink.DownloadCount++;
                await _shareLinkRepository.SaveChangesAsync();

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(shareLink.File.Name, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return (stream, contentType, shareLink.File.Name);
            }

            if (shareLink.FolderId.HasValue)
            {
                shareLink.DownloadCount++;
                await _shareLinkRepository.SaveChangesAsync();

                var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    await AddFolderToArchiveAsync(archive, shareLink.FolderId.Value, shareLink.OwnerId, "");
                }

                memoryStream.Position = 0;

                return (memoryStream, "application/zip", $"{shareLink.Folder!.Name}.zip");
            }

            throw new ValidationException("Invalid share link structure.");
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadSharedItemAsync(string token, Guid fileId, string? password = null, int? targetFolderId = null)
        {
            var folderContent = await GetSharedFolderContentAsync(token, password, targetFolderId);

            var targetItem = folderContent.FirstOrDefault(x => !x.IsFolder && x.OriginalId == fileId.ToString());

            if (targetItem == null)
            {
                throw new ValidationException("File not found in the shared folder or access denied.");
            }

            var shareLink = await _shareLinkRepository.GetByTokenAsync(token);
            var file = await _fileRepository.GetByIdAsync(fileId, shareLink!.OwnerId);

            if (file == null)
                throw new NotFoundException("File", fileId);

            var stream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(file.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            shareLink.DownloadCount++;
            await _shareLinkRepository.SaveChangesAsync();

            return (stream, contentType, file.Name);
        }

        public async Task<IEnumerable<SharedItemDto>> GetSharedFolderContentAsync(string token, string? password = null, int ? folderId = null)
        {
            var shareLink = await _shareLinkRepository.GetByTokenWithRelationsAsync(token);

            if (shareLink == null || (shareLink.ExpirationDate.HasValue && shareLink.ExpirationDate.Value < DateTime.UtcNow))
            {
                throw new NotFoundException("Share link", token);
            }

            if (!shareLink.FolderId.HasValue)
            {
                throw new ValidationException("This share link does not point to a folder.");
            }

            if (!string.IsNullOrEmpty(shareLink.PasswordHash))
            {
                if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, shareLink.PasswordHash))
                {
                    throw new ValidationException("Invalid password for share link.");
                }
            }

            var actualFolderId = folderId ?? shareLink.FolderId.Value;

            if (folderId.HasValue)
            {
                var folderCheck = await _folderRepository.GetByIdAsync(folderId.Value, shareLink.OwnerId).EnsureExistsAsync(() => "Folder not found");
            }

            var folders = await _folderRepository.GetByParentIdAsync(actualFolderId, shareLink.OwnerId);
            var files = await _fileRepository.GetByFolderIdAsync(actualFolderId, shareLink.OwnerId);

            var result = new List<SharedItemDto>();

            foreach (var folder in folders)
            {
                result.Add(new SharedItemDto
                {
                    Id = $"folder_{folder.Id}",
                    OriginalId = folder.Id.ToString(),
                    Name = folder.Name,
                    IsFolder = true,
                    Size = 0,
                    CreatedAt = folder.CreatedAt
                });
            }

            foreach (var file in files)
            {
                result.Add(new SharedItemDto
                {
                    Id = $"file_{file.Id}",
                    OriginalId = file.Id.ToString(),
                    Name = file.Name,
                    IsFolder = false,
                    Size = file.Size,
                    CreatedAt = file.CreatedAt
                });
            }

            return result
                .OrderByDescending(x => x.IsFolder)
                .ThenBy(x => x.Name);
        }

        public async Task<ShareLinkInfoDto> GetShareLinkInfoAsync(string token)
        {
            var shareLink = await _shareLinkRepository.GetByTokenWithRelationsAsync(token);

            if (shareLink == null || (shareLink.ExpirationDate.HasValue && shareLink.ExpirationDate.Value < DateTime.UtcNow))
            {
                throw new NotFoundException("Share link not found or expired.");
            }

            bool isFolder = shareLink.FolderId.HasValue;
            string name = isFolder ? shareLink.Folder!.Name : shareLink.File!.Name;
            long? size = isFolder ? null : shareLink.File!.Size;

            return new ShareLinkInfoDto
            {
                FileName = name,
                IsFolder = isFolder,
                Size = size,
                IsPasswordProtected = !string.IsNullOrEmpty(shareLink.PasswordHash),
                ExpirationDate = shareLink.ExpirationDate
            };
        }

        private async Task AddFolderToArchiveAsync(ZipArchive archive, int folderId, string ownerId, string currentPath)
        {
            var files = await _fileRepository.GetByFolderIdAsync(folderId, ownerId);
            foreach (var file in files)
            {
                var entryName = Path.Combine(currentPath, file.Name);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                using (var entryStream = entry.Open())
                using (var fileStream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath))
                {
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            var subFolders = await _folderRepository.GetByParentIdAsync(folderId, ownerId);
            foreach (var subFolder in subFolders)
            {
                var newPath = Path.Combine(currentPath, subFolder.Name);
                archive.CreateEntry(newPath + "/");
                await AddFolderToArchiveAsync(archive, subFolder.Id, ownerId, newPath);
            }
        }
    }
}
