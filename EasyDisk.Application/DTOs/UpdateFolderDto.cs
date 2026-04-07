using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class UpdateFolderDto
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
    }
}
