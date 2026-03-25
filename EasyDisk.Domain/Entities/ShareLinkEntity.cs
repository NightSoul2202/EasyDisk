namespace EasyDisk.Domain.Entities
{
    public class ShareLinkEntity
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int ClickCount { get; set; } = 0;

        public Guid FileId { get; set; }

        public FileEntity? File { get; set; }
    }
}