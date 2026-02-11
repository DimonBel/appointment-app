using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentBooking.Domain.Models
{
    public class MessageBrokerOptions
    {
        public string RabbitMQUrl { get; set; }

        public string RabbitMQUser { get; set; }

        public string RabbitMQPassword { get; set; }

        public string RabbitMQvirtualHost { get; set; }
    }
}
