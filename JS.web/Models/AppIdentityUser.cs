using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JS.web.Models
{
    public class AppIdentityUser : IdentityUser
    {
        public AppIdentityUser()
        {
            Messages = new HashSet<Message>();
        }

        // One to many relationship
        public virtual ICollection<Message> Messages { get; set; }

    }
}
