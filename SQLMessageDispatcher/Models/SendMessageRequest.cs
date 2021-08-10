namespace SQLMessageDispatcher.Models
{
    public class SendMessageRequest : Amazon.SQS.Model.SendMessageRequest
    {
        // Summary:
        //     Gets and sets the property VisibilityTimeout.
        //     The duration (in seconds) that the received messages are hidden from subsequent
        //     retrieve requests after being retrieved by a
        //     ReceiveMessage
        //     request.
        public int VisibilityTimeout { get; set; }
    }
}
