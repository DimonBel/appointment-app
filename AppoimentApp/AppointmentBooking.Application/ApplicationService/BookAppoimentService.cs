using AppointmentBooking.Domain.Entity;
using AppointmentBooking.Domain.Models;
using AppointmentBooking.Domain.Repository;
using AppointmentBooking.Domain.Service;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.ApplicationService
{
    public class BookAppoimentService:IBookAppoimentService
    {
        private readonly IAppointmentBookingRepository _appointmentBookingRepository;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        public BookAppoimentService(IAppointmentBookingRepository appointmentBookingRepository)
        {
            _appointmentBookingRepository = appointmentBookingRepository;
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            _connection = connectionFactory.CreateConnection("Book-Appoiment-Publish");

            _channel = _connection.CreateModel();
        }

        public void BookAppoiment(AppoimentBookingModel appoimentBookingModel)
        {
            var appoiment = new AppoimentBooking()
            {
                PatientId = appoimentBookingModel.PatientId,
                PatientName = appoimentBookingModel.PatientName,
                ReservedAt = DateTime.Now,
                SlotId = appoimentBookingModel.SlotId,
              
            };
            _appointmentBookingRepository.AddAppoimentBooking(appoiment);
            _appointmentBookingRepository.SaveChanges();

            var payload = JsonConvert.SerializeObject(appoiment);
            var byteArray = Encoding.UTF8.GetBytes(payload);

            _channel.BasicPublish("Book-Appoiment", "Book -Appoiment", null, byteArray);

        }

       
    }
}
