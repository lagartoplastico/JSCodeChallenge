using JS.web.Data;
using JS.web.Hubs;
using JS.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JS.web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppIdentityUser> _userManager;
        private readonly ApplicationDbContext _db;
        private const string NAME_QUEUE = "STOCKQUEUE";
        private const string HOST = "localhost";
        private const string USER = "guest";
        private const string PASSWORD = "guest";

        private readonly IHubContext<ChatHub> _hubContext;

        public HomeController(ILogger<HomeController> logger, UserManager<AppIdentityUser> userManager,
                                ApplicationDbContext db, IHubContext<ChatHub> hubContext)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Chat()
        {
            AppIdentityUser currentUser = await _userManager.GetUserAsync(User);

            if (User.Identity.IsAuthenticated)
            {
                ViewBag.CurrentUserName = currentUser.UserName;
                IEnumerable<Message> messages = _db.Messages.ToList().TakeLast<Message>(5);

                return View(messages);
            }

            return Error();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Message message)
        {
            if (ModelState.IsValid)
            {
                string commandFilter = "/stock=";
                if (message.Text.Length >= commandFilter.Length 
                    && message.Text.Substring(0, commandFilter.Length) == commandFilter )
                {
                    // The (decoupled) Bot API retrieves the stock information and put the formated message in the MQ. 
                    if (await SendToBotAPIAsync(message.Text.Substring(commandFilter.Length)))
                    {
                        await ConsumeMQAsync();
                    }
                }
                else
                {
                    var sender = await _userManager.GetUserAsync(User);
                    message.UserId = sender.Id;
                    message.Timestamp = DateTime.Now;
                    await _db.Messages.AddAsync(message);
                    await _db.SaveChangesAsync();
                }

                return Ok();
            }

            return Error();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<bool> SendToBotAPIAsync(string stockCode)
        {
            string url = "https://localhost:44323/StockBot/" + stockCode;
            try
            {
                using HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    _logger.LogWarning("Cannot retrieve from stock API");
                    
                    await _hubContext.Clients.All.SendAsync("receiveMessage", new Message
                    {
                        Username = "Bot",
                        Text = "*** Invalid Stock_code or API not available ***",
                        Timestamp = DateTime.Now
                    });

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                await _hubContext.Clients.All.SendAsync("receiveMessage", new Message
                {
                    Username = "Bot",
                    Text = "*** Invalid Stock_code or API not available ***",
                    Timestamp = DateTime.Now
                });

                return false;
            }
        }

        private async Task<bool> ConsumeMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = HOST, UserName = USER, Password = PASSWORD };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: NAME_QUEUE, durable: true, exclusive: false,
                    autoDelete: false, arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("--- [x] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += CallBackRecievedMessage;

                channel.BasicConsume(queue: NAME_QUEUE, autoAck: false, consumer: consumer);

                Thread.Sleep(1000);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                await _hubContext.Clients.All.SendAsync("receiveMessage", new Message
                {
                    Username = "Bot",
                    Text = "*** Error retrieving info from MQ ***",
                    Timestamp = DateTime.Now
                });

                return false;
            }
        }

        private void CallBackRecievedMessage(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var message = Encoding.UTF8.GetString(body.ToArray());
            _logger.LogInformation("--- [x] Received {0}", message);

            _hubContext.Clients.All.SendAsync("receiveMessage", new Message
            {
                Username = "Bot",
                Text = message,
                Timestamp = DateTime.Now
            });

            _logger.LogInformation("--- [x] Done");

            // Nofifying ACK of recieved
            ((EventingBasicConsumer)sender)?.Model?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    }
}