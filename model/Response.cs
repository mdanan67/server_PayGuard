using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace server.model
{
    public class Response
    {
        public bool Success { get; set; }
        public string Role { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public dynamic Data { get; set; }
        public List<string> Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }


}