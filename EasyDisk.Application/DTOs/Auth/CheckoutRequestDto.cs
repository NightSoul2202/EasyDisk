using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class CheckoutRequestDto
    {
        [Required]
        public string PlanName { get; set; } = string.Empty;
    }
}
