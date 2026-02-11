using AppoinmentConfirmation.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppoinmentConfirmation
{
    public class NotificationService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string Queue = "notifications-service/Book-Appoinment";

        public NotificationService() {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            _connection = connectionFactory.CreateConnection("notifications-service-Book-Appoinment-consumer");

            _channel = _connection.CreateModel();
        }


        protected void SubscribeNotification(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, eventArgs) =>
            {
                var contentArray = eventArgs.Body.ToArray();
                var contentString = Encoding.UTF8.GetString(contentArray);
                var message = JsonConvert.DeserializeObject<AppoimentBookingModel>(contentString);

                LogInfo("new book Appoinment ", message);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(Queue, false, consumer);

          
        }
        protected void LogInfo(string message, object inputs)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM\\dd")) + ".log";
            string contents = $@"
***********Info Log**********
==={DateTime.Now.ToString("HH:mm:ss")}=======================================================================================
{message}
----------------------------------------
{JsonConvert.SerializeObject(inputs)}
***********End Info Log**********
" + Environment.NewLine;

            File.AppendAllTextAsync(path, contents);
        }
    }
}
