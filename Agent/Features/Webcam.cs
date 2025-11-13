// Features/Webcam.cs
using OpenCvSharp;

public static class Webcam
{
    private static VideoCapture _capture;
    private static bool _streaming = false;

    public static void StartStreaming(HubConnection connection)
    {
        _capture = new VideoCapture(0);
        _streaming = true;

        Task.Run(async () =>
        {
            using var frame = new Mat();
            while (_streaming && _capture.Read(frame))
            {
                var jpg = frame.ToBytes(".jpg");
                await connection.SendAsync("SendToClient", Context.ConnectionId, new
                {
                    webcam_frame = Convert.ToBase64String(jpg)
                });
                await Task.Delay(100); // 10 FPS
            }
        });
    }

    public static void StopStreaming()
    {
        _streaming = false;
        _capture?.Release();
    }
}