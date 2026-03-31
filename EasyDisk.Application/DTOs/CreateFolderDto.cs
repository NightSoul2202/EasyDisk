using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class CreateFolderDto
    {
        [Required(ErrorMessage = "Folder name is required.")]
        [MaxLength(255, ErrorMessage = "Folder name cannot exceed 255 characters.")]
        public string Name { get; set; } = String.Empty;
        public int? ParentFolderId { get; set; }
    }
}
