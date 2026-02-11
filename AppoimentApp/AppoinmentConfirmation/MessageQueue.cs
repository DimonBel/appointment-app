
using AppoinmentConfirmation.Interfaces;
using AppoinmentConfirmation.Models;
using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppoinmentConfirmation
{ 
    public class MessageQueue : IMessageQueue
    {
        protected IBus _bus;
        bool _publisherConfirms;
        private readonly MessageBrokerOptions _options;
        public MessageQueue()
        {
            init();
        }
        public MessageQueue(MessageBrokerOptions options)
        {
            _options = options;
            init();

        }

        private void init()
        {

            string rabbitMQUrl = _options.RabbitMQUrl;
            string userName = _options.RabbitMQUser;
            string password = _options.RabbitMQPassword;
            string vHost = _options.RabbitMQvirtualHost.ToString();

            string connection = string.Format("host={0};virtualHost={1};username={2};password={3};publisherConfirms={4}",
                 rabbitMQUrl, vHost, userName, password, _publisherConfirms);

            _bus = RabbitHutch.CreateBus(connection, (serviceRegister) =>
            {

            });
        }


        public MessageQueue(bool publisherConfirms)
        {
            _publisherConfirms = publisherConfirms;
            init();

        }


       
        public void StartSubscribtion()
        {
            #region VendorSubscrib
            
            _bus.Subscribe<AppoimentBookingModel>("BookAppoiment", SendNotification);
            #endregion
        }
        public void SendNotification(AppoimentBookingModel appoimentBookingModel)
        {


        }
    }
}
