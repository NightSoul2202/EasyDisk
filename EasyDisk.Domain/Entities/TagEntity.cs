using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Domain.Entities
{
    public class TagEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#000000";
        public string OwnerId { get; set; } = string.Empty;

        public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    }
}
