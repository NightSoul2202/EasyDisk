namespace EasyDisk.Domain.Entities
{
    public class ShareLinkEntity
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DownloadCount { get; set; }

        public string OwnerId { get; set; } = string.Empty;

        public Guid? FileId { get; set; }
        public FileEntity? File { get; set; }

        public int? FolderId { get; set; }
        public FolderEntity? Folder { get; set; }

        public bool IsFolderLink => FolderId.HasValue;
        public bool IsFileLink => FileId.HasValue;
    }
}