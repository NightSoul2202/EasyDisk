using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class CreateShareLinkDto
    {
        public Guid FileId { get; set; }
        public string? Password { get; set; }
        public int? ExpirationHours { get; set; }
    }
}
