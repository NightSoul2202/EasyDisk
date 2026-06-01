using EasyDisk.Application.Interfaces.Files;
using EasyDisk.Infrastructure.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aes = System.Security.Cryptography.Aes;

namespace EasyDisk.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly string _uploadDirectory;
        private readonly string _tempDirectory;
        private readonly byte[] _encryptionKey;

        private readonly IMemoryCache _cache;
        private const string CancelledPrefix = "cancel_";

        public FileStorageService(IConfiguration configuration, IMemoryCache cache)
        {
            _basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            
            _uploadDirectory = Path.Combine(_basePath, "Uploads");
            _tempDirectory = Path.Combine(_basePath, "Temp");

            var keyString = configuration["Storage:EncryptionKey"] ?? throw new InvalidOperationException("Encryption key is not configured.");
            _encryptionKey = Encoding.UTF8.GetBytes(keyString);

            _cache = cache;

            if (_encryptionKey.Length != 32)
            {
                throw new InvalidOperationException("Encryption key must be 32 bytes long for AES-256.");
            }

            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
            if(!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public async Task AppendChunkAsync(string uploadId, Stream chunkStream)
        {
            if (_cache.TryGetValue(CancelledPrefix + uploadId, out _))
            {
                return;
            }

            var tempFilePath = Path.Combine(_tempDirectory, $"{uploadId}.tmp");

            using var fileStream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.None);
            await chunkStream.CopyToAsync(fileStream);
        }

        public async Task<string> FinalizeUploadAsync(string uploadId, string extension)
        {
            var tempFilePath = Path.Combine(_tempDirectory, $"{uploadId}.tmp");

            if (!File.Exists(tempFilePath))
            {
                throw new StorageFileNotFoundException($"Temp file not found {uploadId}");
            }

            var finalFileName = $"{Guid.NewGuid()}{extension}";
            var finalFilePath = Path.Combine(_uploadDirectory, finalFileName);

            using (Aes aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                using (var finalFileStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write))
                {
                    await finalFileStream.WriteAsync(aes.IV, 0, aes.IV.Length);

                    using (var encryptor = aes.CreateEncryptor())
                    using (var cryptoStream = new CryptoStream(finalFileStream, encryptor, CryptoStreamMode.Write))
                    using (var tempFileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                    {
                        await tempFileStream.CopyToAsync(cryptoStream);
                    }
                }
            }

            File.Delete(tempFilePath);

            return Path.Combine("Uploads", finalFileName);
        }

        public async Task<Stream> GetFileStreamAsync(string physicalPath)
        {
            var fullPath = Path.Combine(_basePath, physicalPath);

            if(!File.Exists(fullPath))
            {
                throw new StorageFileNotFoundException($"File not found at path: {physicalPath}");
            }

            Stream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] iv = new byte[16];
            await fileStream.ReadAsync(iv, 0, iv.Length);

            Aes aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor();

            return new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
        }

        public Task CancelUploadAsync(string uploadId)
        {
            _cache.Set(CancelledPrefix + uploadId, true, TimeSpan.FromSeconds(5));

            var tempFilePath = Path.Combine(_tempDirectory, $"{uploadId}.tmp");

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            return Task.CompletedTask;
        }

        public Task DeleteFileAsync(string physicalPath)
        {
            var fullPath = Path.Combine(_basePath, physicalPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
