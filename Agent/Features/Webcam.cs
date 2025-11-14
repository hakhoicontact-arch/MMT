// Features/Webcam.cs
using OpenCvSharp;
using System.Text.Json;

namespace RemoteControlAgent.Features
{
    public class Webcam : IAgentFeature
    {
        public string Action => "webcam_on";
        private VideoCapture? _capture;
        private Thread? _thread;
        private bool _running = false;
        private AgentController? _controller;

        public void Execute(JsonElement request, AgentController controller)
        {
            var action = request.GetProperty("action").GetString();
            _controller = controller;

            if (action == "webcam_on")
            {
                StartWebcam();
            }
            else if (action == "webcam_off")
            {
                StopWebcam();
            }
        }

        private void StartWebcam()
        {
            if (_running) return;
            _running = true;

            _capture = new VideoCapture(0);
            if (!_capture.IsOpened())
            {
                _controller?.SendResponse("error", "Không mở được webcam");
                return;
            }

            _thread = new Thread(() =>
            {
                _controller?.SendBinaryStart("webcam");
                using var frame = new Mat();

                while (_running && _capture.Read(frame))
                {
                    Cv2.ImEncode(".jpg", frame, out var buf);
                    var chunkSize = 64 * 1024;
                    for (int i = 0; i < buf.Length; i += chunkSize)
                    {
                        var chunk = buf.Skip(i).Take(chunkSize).ToArray();
                        _controller?.SendBinaryChunk(chunk);
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(100); // ~10 FPS
                }

                _controller?.SendBinaryEnd();
            });

            _thread.Start();
            _controller?.SendResponse("webcam_on", "started");
        }

        private void StopWebcam()
        {
            _running = false;
            _capture?.Release();
            _thread?.Join(1000);
            _controller?.SendResponse("webcam_off", "stopped");
        }
    }
}