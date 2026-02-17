using Microsoft.AspNetCore.SignalR;

namespace AppointmentApp.API.Hubs;

public class OrderHub : Hub
{
    public async Task JoinProfessionalGroup(Guid professionalId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"professional_{professionalId}");
    }

    public async Task LeaveProfessionalGroup(Guid professionalId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"professional_{professionalId}");
    }

    public async Task JoinClientGroup(Guid clientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{clientId}");
    }

    public async Task LeaveClientGroup(Guid clientId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"client_{clientId}");
    }

    public async Task JoinOrderGroup(Guid orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }

    public async Task LeaveOrderGroup(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }
}