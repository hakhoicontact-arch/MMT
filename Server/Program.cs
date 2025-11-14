using RemoteControlServer.Hubs;
using RemoteControlServer.Connections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<AgentManager>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetIsOriginAllowed(_ => true)));

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();
app.UseRouting();

app.MapHub<ControlHub>("/control");
app.MapHub<ControlHub>("/agent");

app.MapFallbackToFile("index.html");

app.Run("http://0.0.0.0:8080");