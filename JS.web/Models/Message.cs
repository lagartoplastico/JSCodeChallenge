using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JS.web.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }

        public string UserId { get; set; } // Foreign Key.

        public virtual AppIdentityUser Sender { get; set; }
    }
}
