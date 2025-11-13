// Hubs/ControlHub.cs
using Microsoft.AspNetCore.SignalR;
using RemoteControlServer.Connections;

public class ControlHub : Hub
{
    private readonly AgentManager _agentManager;

    public ControlHub(AgentManager agentManager)
    {
        _agentManager = agentManager;
    }

    // Agent kết nối → đăng ký ID
    public override Task OnConnectedAsync()
    {
        var agentId = Context.GetHttpContext()?.Request.Query["agentId"];
        if (!string.IsNullOrEmpty(agentId))
        {
            _agentManager.RegisterAgent(Context.ConnectionId, agentId);
            Console.WriteLine($"Agent {agentId} connected: {Context.ConnectionId}");
        }
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var agentId = _agentManager.GetOnlineAgents()
            .FirstOrDefault(a => _agentManager.GetAgentConnectionId(a) == Context.ConnectionId);
        if (agentId != null) _agentManager.RemoveAgent(agentId);
        return base.OnDisconnectedAsync(exception);
    }

    // Client gửi lệnh → Server → Agent
    public async Task SendToAgent(string agentId, object message)
    {
        var connectionId = _agentManager.GetAgentConnectionId(agentId);
        if (connectionId != null)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveCommand", message);
        }
    }

    // Agent gửi phản hồi → Server → Client
    public async Task SendToClient(string clientId, object message)
    {
        await Clients.Client(clientId).SendAsync("ReceiveResponse", message);
    }
}