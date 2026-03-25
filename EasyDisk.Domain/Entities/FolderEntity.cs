using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Domain.Entities
{
    public class FolderEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        public int? ParentFolderId { get; set; }
        public string OwnerId { get; set; } = string.Empty;

        public FolderEntity? ParentFolder { get; set; }
        public ICollection<FolderEntity> Subfolders { get; set; } = new List<FolderEntity>();
        public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    }
}
