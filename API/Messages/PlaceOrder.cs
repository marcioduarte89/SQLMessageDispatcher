using SQLMessageDispatcher.Interfaces;

namespace API.Messages
{
    public class PlaceOrder : IMessage
    {
        public string PropertyOne { get; set; }

        public string PropertyTwo { get; set; }
    }
}
