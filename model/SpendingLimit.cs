using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace server.model
{
    public class SpendingLimit
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public double Food { get; set; }
        public double Education { get; set; }
        public double Transport { get; set; }
        public double Entertainment { get; set; }
        public double Shopping { get; set; }
        public double Subscriptions { get; set; }
        public double Mobile { get; set; }
        public double Others { get; set; }

    }
}