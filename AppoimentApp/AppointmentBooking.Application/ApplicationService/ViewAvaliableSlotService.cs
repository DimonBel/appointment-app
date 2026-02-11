using AppointmentBooking.Domain.Models;
using AppointmentBooking.Domain.Service;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace AppointmentBooking.Application.ApplicationService
{
    public class ViewAvaliableSlotService : IViewAvaliableSlotService
    {
        private readonly HttpClient _httpClient;

        public ViewAvaliableSlotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public List<SlotModel> ViewAvaliableSlot()
        {
            // DoctorAvailability.Api exposes: GET /api/DoctorSlot/available
            var result = _httpClient.GetFromJsonAsync<List<SlotModel>>("/api/DoctorSlot/available")
                .GetAwaiter()
                .GetResult();

            return result ?? new List<SlotModel>();

        }

    }
}
