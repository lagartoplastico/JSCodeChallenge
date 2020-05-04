using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BotSotckProducer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockBotController : ControllerBase
    {
        private readonly ILogger<StockBotController> _logger;
        private IConfiguration _config;
        private readonly string rabbitMQHost;
        private readonly string rabbitMQUser;
        private readonly string rabbitMQPass;
        private readonly string rabbitMQQueueName;

        public StockBotController(ILogger<StockBotController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            rabbitMQHost = _config.GetSection("RabbitConnectionInfo:Host").Value;
            rabbitMQUser = _config.GetSection("RabbitConnectionInfo:User").Value;
            rabbitMQPass = _config.GetSection("RabbitConnectionInfo:Password").Value;
            rabbitMQQueueName = _config.GetSection("RabbitConnectionInfo:QueueName").Value;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetAsync(string id)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(String.Format("https://stooq.com/q/l/?s={0}&f=sd2t2ohlcv&h&e=csv", id));
                if (response.IsSuccessStatusCode)
                {
                    var result = await ProcessMessage(response);

                    if (result)
                    {
                        return Ok(new { message = "The message was sent to the MQ" });
                    }
                    else
                    {
                        return StatusCode(500);
                    }
                }
                else
                {
                    return BadRequest(new { error = $"{response.StatusCode} Cannot get CSV file" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending message to the MQ");
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<bool> ProcessMessage(HttpResponseMessage response)
        {
            var streamData = await response.Content.ReadAsStreamAsync();

            StreamReader readerStream = new StreamReader(streamData, Encoding.UTF8);

            string line, lastline = null;

            while ((line = readerStream.ReadLine()) != null)
            {
                lastline = line;
            }

            string message;

            if (String.IsNullOrWhiteSpace(lastline))
            {
                message = $"Information about the stock is in blank.";
            }
            else
            {
                var data = lastline.Split(',');
                if (data[4] == "N/D")
                {
                    return false;
                }
                message = $"{data[0]} quote is ${data[4]} per share";
            }

            if (SendMessageToRabbitMQ(message))
            {
                return true;
            }
            return false;
        }

        private bool SendMessageToRabbitMQ(string message)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = rabbitMQHost,
                    UserName = rabbitMQUser,
                    Password = rabbitMQPass

                };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: rabbitMQQueueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                                  routingKey: rabbitMQQueueName,
                                  basicProperties: properties,
                                  body: body);

                _logger.LogInformation(" Message sent to MQ: {0}", message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message}");
                return false;
            }
        }
    }
}