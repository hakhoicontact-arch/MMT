// Models/AgentConnection.cs
namespace RemoteControlServer.Models
{
    public class AgentConnection
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string MachineName { get; set; } = Environment.MachineName;
        public DateTime ConnectedAt { get; set; } = DateTime.Now;
    }
}