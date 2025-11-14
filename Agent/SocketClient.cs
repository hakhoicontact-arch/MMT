// SocketClient.cs
using System.Text;
using WebSocketSharp;

namespace RemoteControlAgent
{
    public class SocketClient
    {
        private readonly string _url;
        private WebSocket _ws;

        public event Action<string>? OnMessageReceived;
        public event Action? OnConnected;
        public event Action? OnDisconnected;

        public SocketClient(string url)
        {
            _url = url;
        }

        public async Task ConnectAsync()
        {
            _ws = new WebSocket(_url);
            _ws.OnMessage += (sender, e) =>
            {
                if (e.IsBinary)
                {
                    OnMessageReceived?.Invoke(Convert.ToBase64String(e.RawData));
                }
                else
                {
                    OnMessageReceived?.Invoke(e.Data);
                }
            };
            _ws.OnOpen += (sender, e) => OnConnected?.Invoke();
            _ws.OnClose += (sender, e) => OnDisconnected?.Invoke();
            _ws.OnError += (sender, e) => Console.WriteLine($"Lỗi WS: {e.Message}");

            _ws.Connect();
            Console.WriteLine($"Đã kết nối tới {_url}");
        }

        public void Send(string message)
        {
            if (_ws?.IsAlive == true)
                _ws.Send(message);
        }

        public void SendBinary(byte[] data)
        {
            if (_ws?.IsAlive == true)
                _ws.Send(data);
        }

        public void Disconnect()
        {
            _ws?.Close();
        }
    }
}