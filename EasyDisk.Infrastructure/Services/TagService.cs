using EasyDisk.Application.DTOs;
using EasyDisk.Application.DTOs.Tag;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces.Auth;
using EasyDisk.Application.Interfaces.Files;
using EasyDisk.Application.Interfaces.Tag;
using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileRepository _fileRepository;

        public TagService(ITagRepository tagRepository, ICurrentUserService currentUserService, IFileRepository fileRepository)
        {
            _tagRepository = tagRepository;
            _currentUserService = currentUserService;
            _fileRepository = fileRepository;
        }

        public async Task<TagResponseDto> CreateTagAsync(CreateTagDto dto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to create a tag.");

            var existingTag = await _tagRepository.GetByNameAsync(dto.Name, userId).EnsureExistsNameAsync(() => $"Tag with name {dto.Name} already exists.");

            var tag = new TagEntity
            {
                Name = dto.Name,
                Color = dto.Color,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _tagRepository.AddAsync(tag);
            await _tagRepository.SaveChangesAsync();

            return new TagResponseDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<IEnumerable<TagResponseDto>> GetUserTagsAsync()
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to view tags.");

            var tags = await _tagRepository.GetAllByOwnerAsync(userId);

            return tags.Select(t => new TagResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                CreatedAt = t.CreatedAt
            });
        }

        public async Task AttachTagToFileAsync(Guid fileId, int tagId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to attach tag to file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            var tag = await _tagRepository.GetByIdAsync(tagId, userId).EnsureExistsAsync(() => $"Tag with id {tagId} not found.");

            if (!file.Tags.Any(t => t.Id == tagId))
            {
                file.Tags.Add(tag);
                await _fileRepository.SaveChangesAsync();
            }
        }

        public async Task DetachTagFromFileAsync(Guid fileId, int tagId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to detach tag from file.");

            var file = await _fileRepository.GetByIdWithTagsAsync(fileId, userId).EnsureExistsAsync(() => $"File with id {fileId} not found.");

            var tag = await _tagRepository.GetByIdAsync(tagId, userId).EnsureExistsAsync(() => $"Tag with id {tagId} not found.");

            if (file.Tags.Any(t => t.Id == tagId))
            {
                file.Tags.Remove(tag);
                await _fileRepository.SaveChangesAsync();
            }
        }

        public async Task<TagResponseDto> UpdateTagAsync(int tagId, UpdateTagDto dto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to update a tag.");

            var tag = await _tagRepository.GetByIdAsync(tagId, userId).EnsureExistsAsync(() => $"Tag with id {tagId} not found.");

            if (!string.Equals(tag.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var existingTag = await _tagRepository.GetByNameAsync(dto.Name, userId).EnsureExistsNameAsync(() => $"Tag with name {dto.Name} already exists.");
            }

            tag.Name = dto.Name;
            tag.Color = dto.Color;

            await _tagRepository.SaveChangesAsync();

            return new TagResponseDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Color = tag.Color,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task DeleteTagAsync(int tagId)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to delete a tag.");

            var tag = await _tagRepository.GetByIdAsync(tagId, userId).EnsureExistsAsync(() => $"Tag with id {tagId} not found.");

            _tagRepository.Delete(tag);
            await _tagRepository.SaveChangesAsync();
        }
    }
}
