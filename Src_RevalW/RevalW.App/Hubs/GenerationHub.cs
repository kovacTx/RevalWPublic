using Microsoft.AspNetCore.SignalR;

namespace RevalW.App.Hubs;

public class GenerationHub : Hub
{
    public Task JoinGroup(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return Task.CompletedTask;
        return Groups.AddToGroupAsync(Context.ConnectionId, operationId);
    }

    public Task LeaveGroup(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return Task.CompletedTask;
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);
    }
}