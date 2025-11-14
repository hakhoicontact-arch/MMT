// Connections/AgentManager.cs
using Microsoft.AspNetCore.SignalR;

namespace RemoteControlServer.Connections
{
    public class AgentManager
    {
        
        private readonly Dictionary<string, string> _agents = new(); // connectionId -> agentId

        private readonly IHubContext<RemoteControlServer.Hubs.ControlHub> _hubContext;

        public AgentManager(IHubContext<RemoteControlServer.Hubs.ControlHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void RegisterAgent(string connectionId, string agentId)
        {
            _agents[connectionId] = agentId;
            Console.WriteLine($"Agent [{agentId}] đăng ký: {connectionId}");
            BroadcastToClients(new { type = "agent_connected", agentId });
        }

        public void UnregisterAgent(string connectionId)
        {
            if (_agents.TryGetValue(connectionId, out var agentId))
            {
                _agents.Remove(connectionId);
                Console.WriteLine($"Agent [{agentId}] ngắt kết nối");
                BroadcastToClients(new { type = "agent_disconnected", agentId });
            }
        }

        public string? GetAgentConnectionId(string agentId)
        {
            return _agents.FirstOrDefault(x => x.Value == agentId).Key;
        }

        public IEnumerable<string> GetOnlineAgents()
        {
            return _agents.Values.Distinct();
        }

        public void ForwardToAgent(string agentId, object message)
        {
            var connectionId = GetAgentConnectionId(agentId);
            if (connectionId != null)
            {
                _hubContext.Clients.Client(connectionId).SendAsync("receive", message);
            }
        }

        public void ForwardToAgentBinary(string agentId, byte[] data)
        {
            var connectionId = GetAgentConnectionId(agentId);
            if (connectionId != null)
            {
                _hubContext.Clients.Client(connectionId).SendAsync("receiveBinary", data);
            }
        }

        public void BroadcastToClients(object message)
        {
            _hubContext.Clients.All.SendAsync("broadcast", message);
        }

        public void SendToClient(string connectionId, object message)
        {
            _hubContext.Clients.Client(connectionId).SendAsync("receive", message);
        }

        public void SendToClientBinary(string connectionId, byte[] data)
        {
            _hubContext.Clients.Client(connectionId).SendAsync("receiveBinary", data);
        }
    }
}