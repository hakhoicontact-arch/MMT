// Hubs/ControlHub.cs
using Microsoft.AspNetCore.SignalR;
using RemoteControlServer.Connections;
using System.Text.Json;

namespace RemoteControlServer.Hubs
{
    public class ControlHub : Hub
    {
        private readonly AgentManager _agentManager;

        public ControlHub(AgentManager agentManager)
        {
            _agentManager = agentManager;
        }

        public override Task OnConnectedAsync()
        {
            var role = Context.GetHttpContext()?.Request.Query["role"].ToString();
            var agentId = Context.GetHttpContext()?.Request.Query["agentId"].ToString() ?? "unknown";

            if (role == "agent")
            {
                _agentManager.RegisterAgent(Context.ConnectionId, agentId);
            }
            else
            {
                // Client web
                SendOnlineAgents();
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var role = Context.GetHttpContext()?.Request.Query["role"].ToString();
            if (role == "agent")
            {
                _agentManager.UnregisterAgent(Context.ConnectionId);
            }
            return base.OnDisconnectedAsync(exception);
        }

        // Client gửi lệnh → Server → Agent
        public async Task SendCommand(object command)
        {
            var json = command.ToString();
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var doc = JsonDocument.Parse(json);
                var action = doc.RootElement.GetProperty("action").GetString();

                // Xác định agentId từ client (hoặc mặc định dùng first agent)
                var agentId = "PC1"; // Có thể mở rộng chọn từ dropdown

                if (action != null)
                {
                    _agentManager.ForwardToAgent(agentId, command);
                    Console.WriteLine($"Đã chuyển lệnh [{action}] tới Agent [{agentId}]");
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("error", ex.Message);
            }
        }

        // Agent gửi phản hồi → Server → Client
        [HubMethodName("receive")]
        public async Task ReceiveFromAgent(object message)
        {
            await Clients.AllExcept(Context.ConnectionId).SendAsync("receive", message);
        }

        // Agent gửi binary → Server → Client
        [HubMethodName("receiveBinary")]
        public async Task ReceiveBinaryFromAgent(byte[] data)
        {
            await Clients.AllExcept(Context.ConnectionId).SendAsync("receiveBinary", data);
        }

        // Gửi danh sách Agent online về Client
        private async void SendOnlineAgents()
        {
            var agents = _agentManager.GetOnlineAgents().ToArray();
            await Clients.Caller.SendAsync("agents", agents);
        }
    }
}