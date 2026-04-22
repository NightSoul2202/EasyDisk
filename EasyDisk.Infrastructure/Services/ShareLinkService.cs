using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Repositories;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            var userId = GetCurrentUserId();
            ValidateCreationRules(dto);

            var shareLink = await BuildAndSaveShareLinkAsync(dto, userId);

            return MapToResponseDto(shareLink);
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadSharedFileAsync(string token, string? password = null)
        {
            var shareLink = await GetValidatedShareLinkAsync(token, password, requireFile: true);
            return await GetStreamAndContentTypeAsync(shareLink.File!);
        }

        public async Task<string> PrepareSharedFolderZipAsync(string token, string? password = null)
        {
            var shareLink = await GetValidatedShareLinkAsync(token, password, requireFolder: true);

            return await GenerateTempZipAsync(shareLink);
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadSharedItemAsync(string token, Guid fileId, string? password = null, int? targetFolderId = null)
        {
            var shareLink = await GetValidatedShareLinkAsync(token, password, requireFolder: true);

            var file = await ValidateAndGetFolderItemAsync(shareLink, fileId, targetFolderId);

            return await GetStreamAndContentTypeAsync(file);
        }

        public async Task<IEnumerable<SharedItemDto>> GetSharedFolderContentAsync(string token, string? password = null, int? folderId = null)
        {
            var shareLink = await GetValidatedShareLinkAsync(token, password, requireFolder: true);

            return await FetchAndMapFolderContentAsync(shareLink, folderId);
        }

        public async Task<ShareLinkInfoDto> GetShareLinkInfoAsync(string token)
        {
            var shareLink = await FetchShareLinkOrThrowAsync(token);

            return MapToInfoDto(shareLink);
        }


        private string GetCurrentUserId()
        {
            return _currentUserService.UserId ?? throw new ValidationException("User must be authenticated.");
        }

        private void ValidateCreationRules(CreateShareLinkDto dto)
        {
            if (dto.FileId == null && dto.FolderId == null)
            {
                throw new ValidationException("You must provide either a FileId or a FolderId.");
            }
                
            if (dto.FileId != null && dto.FolderId != null)
            {
                throw new ValidationException("Cannot share both a file and a folder in a single link.");
            }
        }

        private async Task<ShareLinkEntity> BuildAndSaveShareLinkAsync(CreateShareLinkDto dto, string userId)
        {
            var shareLink = new ShareLinkEntity
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N").Substring(0, 12),
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpirationDate = dto.ExpirationHours.HasValue ? DateTime.UtcNow.AddHours(dto.ExpirationHours.Value) : null,
                PasswordHash = !string.IsNullOrEmpty(dto.Password) ? BCrypt.Net.BCrypt.HashPassword(dto.Password) : null,
                FileId = dto.FileId,
                FolderId = dto.FolderId
            };

            await VerifyLinkTargetExistsAsync(shareLink, userId);

            await _shareLinkRepository.AddAsync(shareLink);
            await _shareLinkRepository.SaveChangesAsync();

            return shareLink;
        }

        private async Task VerifyLinkTargetExistsAsync(ShareLinkEntity link, string userId)
        {
            if (link.FileId.HasValue)
            {
                _ = await _fileRepository.GetByIdAsync(link.FileId.Value, userId) ?? throw new NotFoundException("File", link.FileId.Value);
            }
                
            if (link.FolderId.HasValue)
            {
                _ = await _folderRepository.GetByIdAsync(link.FolderId.Value, userId) ?? throw new NotFoundException("Folder", link.FolderId.Value);
            }
        }

        private async Task<ShareLinkEntity> GetValidatedShareLinkAsync(string token, string? password, bool requireFolder = false, bool requireFile = false)
        {
            var link = await FetchShareLinkOrThrowAsync(token);

            if (!string.IsNullOrEmpty(link.PasswordHash) && (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, link.PasswordHash)))
            {
                throw new ValidationException("Invalid password for share link.");
            }

            if (requireFolder && !link.FolderId.HasValue)
            {
                throw new ValidationException("This share link does not point to a folder.");
            }

            if (requireFile && !link.FileId.HasValue)
            {
                throw new ValidationException("This share link does not point to a file.");
            }

            return link;
        }

        private async Task<ShareLinkEntity> FetchShareLinkOrThrowAsync(string token)
        {
            var link = await _shareLinkRepository.GetByTokenWithRelationsAsync(token);

            if (link == null || (link.ExpirationDate.HasValue && link.ExpirationDate.Value < DateTime.UtcNow))
            {
                throw new NotFoundException("Share link not found or expired.");
            }
                
            return link;
        }

        private async Task<string> GenerateTempZipAsync(ShareLinkEntity shareLink)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                await AddFolderToArchiveAsync(archive, shareLink.FolderId!.Value, shareLink.OwnerId, "");
            }

            shareLink.DownloadCount++;
            await _shareLinkRepository.SaveChangesAsync();

            return Path.GetFileName(tempPath);
        }

        private async Task<FileEntity> ValidateAndGetFolderItemAsync(ShareLinkEntity shareLink, Guid fileId, int? targetFolderId)
        {
            var actualFolderId = targetFolderId ?? shareLink.FolderId!.Value;
            var filesInFolder = await _fileRepository.GetByFolderIdAsync(actualFolderId, shareLink.OwnerId);

            var file = filesInFolder.FirstOrDefault(f => f.Id == fileId)
                ?? throw new ValidationException("File not found in the shared folder or access denied.");

            shareLink.DownloadCount++;
            await _shareLinkRepository.SaveChangesAsync();

            return file;
        }

        private async Task<(Stream, string, string)> GetStreamAndContentTypeAsync(FileEntity file)
        {
            var stream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath);
            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(file.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return (stream, contentType, file.Name);
        }

        private async Task<IEnumerable<SharedItemDto>> FetchAndMapFolderContentAsync(ShareLinkEntity shareLink, int? folderId)
        {
            var actualFolderId = folderId ?? shareLink.FolderId!.Value;

            if (folderId.HasValue)
            {
                _ = await _folderRepository.GetByIdAsync(folderId.Value, shareLink.OwnerId).EnsureExistsAsync(() => "Folder not found");
            }
                
            var folders = await _folderRepository.GetByParentIdAsync(actualFolderId, shareLink.OwnerId);
            var files = await _fileRepository.GetByFolderIdAsync(actualFolderId, shareLink.OwnerId);

            var items = folders.Select(f => new SharedItemDto 
            { 
                Id = $"folder_{f.Id}", 
                OriginalId = f.Id.ToString(), 
                Name = f.Name, 
                IsFolder = true, 
                Size = 0, 
                CreatedAt = f.CreatedAt 
            })
            .Concat(files.Select(f => new SharedItemDto 
            { 
                Id = $"file_{f.Id}", 
                OriginalId = f.Id.ToString(),
                Name = f.Name,
                IsFolder = false, 
                Size = f.Size, 
                CreatedAt = f.CreatedAt 
            }));

            return items.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name);
        }

        private ShareLinkResponseDto MapToResponseDto(ShareLinkEntity link) => new()
        {
            Token = link.Token,
            DownloadUrl = $"/api/Share/s/{link.Token}",
            ExpirationDate = link.ExpirationDate,
            IsPasswordProtected = !string.IsNullOrEmpty(link.PasswordHash)
        };

        private ShareLinkInfoDto MapToInfoDto(ShareLinkEntity link)
        {
            bool isFolder = link.FolderId.HasValue;
            return new ShareLinkInfoDto
            {
                FileName = isFolder ? link.Folder!.Name : link.File!.Name,
                IsFolder = isFolder,
                Size = isFolder ? null : link.File!.Size,
                IsPasswordProtected = !string.IsNullOrEmpty(link.PasswordHash),
                ExpirationDate = link.ExpirationDate
            };
        }

        private async Task AddFolderToArchiveAsync(ZipArchive archive, int folderId, string ownerId, string currentPath)
        {
            var files = await _fileRepository.GetByFolderIdAsync(folderId, ownerId);
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(Path.Combine(currentPath, file.Name), CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = await _fileStorageService.GetFileStreamAsync(file.PhysicalPath);
                await fileStream.CopyToAsync(entryStream);
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