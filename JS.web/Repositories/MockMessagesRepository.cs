using JS.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JS.web.Repositories
{
    public class MockMessagesRepository : IMessagesRepository
    {
        private List<Message> fakeDb;
        private Message fakeMessage;
        public MockMessagesRepository()
        {
            fakeDb = new List<Message>();
            fakeMessage = new Message
            {
                Username = "fake",
                Text = "mock mock mock"
            };

            for (int i = 0; i < 50; i++)
            {
                fakeDb.Add(fakeMessage);
            }

        }
        public Task<Message> CreateAsync(Message m)
        {
            fakeDb.Add(m);
            return Task.FromResult(m);
        }

        public IEnumerable<Message> RetrieveMessagesToShow()
        {
            return fakeDb;
        }
    }
}
