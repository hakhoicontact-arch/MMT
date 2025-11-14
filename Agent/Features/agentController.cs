// Features/AgentController.cs
using System.Text.Json;

namespace RemoteControlAgent.Features
{
    public class AgentController
    {
        private readonly SocketClient _client;
        private readonly Dictionary<string, IAgentFeature> _features = new();

        public AgentController(SocketClient client)
        {
            _client = client;
            _client.OnMessageReceived += HandleMessage;
            _client.OnConnected += () => SendResponse("connected", "Agent ready");
        }

        public void RegisterFeature(IAgentFeature feature)
        {
            _features[feature.Action] = feature;
        }

        private void HandleMessage(string message)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(message);
                var action = json.GetProperty("action").GetString();

                if (_features.TryGetValue(action!, out var feature))
                {
                    feature.Execute(json, this);
                }
            }
            catch (Exception ex)
            {
                SendResponse("error", ex.Message);
            }
        }

        public void SendResponse(string action, object data)
        {
            var response = new { action, response = data };
            _client.Send(JsonSerializer.Serialize(response));
        }

        public void SendUpdate(string update)
        {
            var msg = new { update };
            _client.Send(JsonSerializer.Serialize(msg));
        }

        public void SendBinaryStart(string type)
        {
            var msg = new { binary_start = type };
            _client.Send(JsonSerializer.Serialize(msg));
        }

        public void SendBinaryChunk(byte[] chunk)
        {
            _client.SendBinary(chunk);
        }

        public void SendBinaryEnd()
        {
            var msg = new { binary_end = true };
            _client.Send(JsonSerializer.Serialize(msg));
        }
    }

    public interface IAgentFeature
    {
        string Action { get; }
        void Execute(JsonElement request, AgentController controller);
    }
}