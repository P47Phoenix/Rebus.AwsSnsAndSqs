using System;

namespace Topic.Contracts
{
    public class MessengerMessage
    {
        public string Message { get; set; }

        public DateTime CreateDateTime { get; set; }
        public string Sender { get; set; }
    }
}
