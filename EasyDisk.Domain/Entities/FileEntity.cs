using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Domain.Entities
{
    public class FileEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string PhysicalPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public int? FolderId { get; set; }

        public FolderEntity? Folder { get; set; }
        public ICollection<ShareLinkEntity> ShareLinks { get; set; } = new List<ShareLinkEntity>();
        public ICollection<FileVersionEntity> Versions { get; set; } = new List<FileVersionEntity>();
        public ICollection<TagEntity> Tags { get; set; } = new List<TagEntity>();
    }
}
