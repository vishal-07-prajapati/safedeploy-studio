using Microsoft.AspNetCore.SignalR;

namespace SafeDeployStudio.Api.Hubs;

public class DeploymentHub : Hub
{
    public async Task JoinDeploymentGroup(string deploymentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"deployment-{deploymentId}");
    }

    public async Task LeaveDeploymentGroup(string deploymentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"deployment-{deploymentId}");
    }
}
