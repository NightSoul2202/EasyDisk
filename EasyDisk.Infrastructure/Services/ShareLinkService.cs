using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class ShareLinkService : IShareLinkService
    {
        private readonly IShareLinkRepository _shareLinkRepository;
        private readonly IFileRepository _fileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;

        public ShareLinkService(
            IShareLinkRepository shareLinkRepository,
            IFileRepository fileRepository,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService)
        {
            _shareLinkRepository = shareLinkRepository;
            _fileRepository = fileRepository;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
        }
        
        public async Task<ShareLinkResponseDto> CreateShareLinkAsync(CreateShareLinkDto dto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to create a share link.");

            var file = await _fileRepository.GetByIdAsync(dto.FileId, userId).EnsureExistsAsync(() => $"File with id {dto.FileId} not found.");

            var token = Guid.NewGuid().ToString("N").Substring(0, 12);

            var shareLink = new ShareLinkEntity
            {
                Id = Guid.NewGuid(),
                Token = token,
                FileId = dto.FileId,
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
            var shareLink = await _shareLinkRepository.GetByTokenAsync(token);

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

        public async Task<ShareLinkInfoDto> GetShareLinkInfoAsync(string token)
        {
            var shareLink = await _shareLinkRepository.GetByTokenAsync(token);

            if (shareLink == null || (shareLink.ExpirationDate.HasValue && shareLink.ExpirationDate.Value < DateTime.UtcNow))
            {
                throw new NotFoundException("Share link not found or expired.");
            }

            return new ShareLinkInfoDto
            {
                FileName = shareLink.File!.Name,
                Size = shareLink.File.Size,
                IsPasswordProtected = !string.IsNullOrEmpty(shareLink.PasswordHash),
                ExpirationDate = shareLink.ExpirationDate
            };
        }
    }
}
