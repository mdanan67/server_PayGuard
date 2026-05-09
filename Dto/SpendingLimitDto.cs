using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace server.Dto
{
    public class SpendingLimitDto
    {
        [Required(ErrorMessage = "ChildId is required")]
        public Guid ChildId { get; set; }
        public double? Food { get; set; }
        public double? Education { get; set; }
        public double? Transport { get; set; }
        public double? Entertainment { get; set; }
        public double? Shopping { get; set; }
        public double? Subscriptions { get; set; }
        public double? Mobile { get; set; }
        public double? Others { get; set; }
    }

}
