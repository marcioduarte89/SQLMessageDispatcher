using Amazon.SQS;
using API.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SQSMessageDispatcher.Extensions;
using System.Threading.Tasks;

namespace API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {

        private readonly IAmazonSQS amazonSqs;
        private readonly string _queueName;

        public OrderController(IConfiguration configuration, IAmazonSQS amazonSqs)
        {

            this.amazonSqs = amazonSqs;
            _queueName = configuration.GetValue<string>("AWS:Queue");
        }

        [HttpPost]
        public async Task Post()
        {
            for (int i = 0; i < 100; i++)
            {
                //var sendMessageRequest = new SQSMessageDispatcher.Models.SendMessageRequest()
                //{
                //    VisibilityTimeout = 1800,
                //    QueueUrl = _queueName,
                //    MessageBody = JsonConvert.SerializeObject(new PlaceOrder()
                //    {
                //        PropertyOne = "Property one",
                //        PropertyTwo = "Property two",
                //    })
                //};

                // await amazonSqs.SendMessageAsync<PlaceOrder>(sendMessageRequest);

                await amazonSqs.SendMessageAsync(_queueName, new PlaceOrder()
                {
                    PropertyOne = "Property one",
                    PropertyTwo = "Property two",
                });
            }
        }
    }
}
