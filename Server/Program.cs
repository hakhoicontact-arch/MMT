// Program.cs
using RemoteControlServer.Hubs;
using RemoteControlServer.Connections;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSignalR();
builder.Services.AddSingleton<AgentManager>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseStaticFiles(); // phục vụ Client từ wwwroot
app.UseRouting();

app.MapHub<ControlHub>("/controlhub");

app.MapGet("/", () => "Server is running. Go to /index.html");

app.Run();