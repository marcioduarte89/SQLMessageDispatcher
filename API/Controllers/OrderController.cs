using Amazon.SQS;
using API.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SQLMessageDispatcher.Extensions;
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
			await amazonSqs.SendMessageAsync(_queueName, new PlaceOrder()
			{
				PropertyOne = "Property one",
				PropertyTwo = "Property two",
			});
        }
    }
}
