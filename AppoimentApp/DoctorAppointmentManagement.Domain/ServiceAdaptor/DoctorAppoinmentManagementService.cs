using DoctorAppointmentManagement.Domain.RepositoryPort;
using DoctorAppointmentManagement.Domain.ServicePort;
using System.Net.Http;

namespace DoctorAppointmentManagement.Domain.ServiceAdaptor
{
    public class DoctorAppoinmentManagementService : IDoctorAppoinmentManagementService
    {
        private readonly IFN_GetUpcomingAppoinmentRepository _fN_GetUpcomingAppoinmentRepository;
        private readonly HttpClient _httpClient;
        public DoctorAppoinmentManagementService(IFN_GetUpcomingAppoinmentRepository fN_GetUpcomingAppoinmentRepository)
        {
            _fN_GetUpcomingAppoinmentRepository = fN_GetUpcomingAppoinmentRepository;
        }

        public DoctorAppoinmentManagementService(
            IFN_GetUpcomingAppoinmentRepository fN_GetUpcomingAppoinmentRepository,
            HttpClient httpClient)
        {
            _fN_GetUpcomingAppoinmentRepository = fN_GetUpcomingAppoinmentRepository;
            _httpClient = httpClient;
        }
        public IQueryable<FN_GetUpcomingAppoinment> UpcomingAppointments()
        {
            return _fN_GetUpcomingAppoinmentRepository.GetUpcomingAppoinment();
        }
        public void CancelAppointments(Guid SlotId)
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException("HttpClient is not configured. Ensure DI registers DoctorAppoinmentManagementService with HttpClient.");
            }

            var requestUrl = $"/api/ChangeAppoinmentStatus?SlotId={SlotId}&StatusId=2";
            var response = _httpClient.PutAsync(requestUrl, content: null).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        public void UpdateAppointmentToComplete(Guid SlotId)
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException("HttpClient is not configured. Ensure DI registers DoctorAppoinmentManagementService with HttpClient.");
            }

            var requestUrl = $"/api/ChangeAppoinmentStatus?SlotId={SlotId}&StatusId=1";
            var response = _httpClient.PutAsync(requestUrl, content: null).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }
    }
}
