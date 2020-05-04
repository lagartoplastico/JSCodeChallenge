using JS.web.Controllers;
using JS.web.Models;
using JS.web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Xunit;

namespace JS.unitTests
{
    public class OkResult
    {
        [Fact]
        public async Task Test1()
        {
            // Arrange
            var mockRepo = new MockMessagesRepository();
            var mockConfig = new MockConfig();
            var controller = new HomeController(null, null, null, mockConfig, mockRepo);
            var m = new Message
            {
                Username = "test",
                Text = "test"
            };
            // Act
            var result = await controller.Create(m);

            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkResult>(result);
        }
    }
}