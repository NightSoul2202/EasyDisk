using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Tag
{
    public class CreateTagDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#000000";
    }
}
