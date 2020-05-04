using JS.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JS.web.Repositories
{
    public interface IMessagesRepository
    {
        Task<Message> CreateAsync(Message m);

        IEnumerable<Message> RetrieveMessagesToShow();
    }
}
