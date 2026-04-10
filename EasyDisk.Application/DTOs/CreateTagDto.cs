using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class CreateTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#000000";
    }
}
