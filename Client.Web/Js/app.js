const connection = new signalR.HubConnectionBuilder()
    .withUrl("/controlhub")
    .build();

let currentAgentId = "PC-Khiem"; // thay bằng input chọn Agent

connection.start().then(() => {
    console.log("Client connected");
});

// Nhận phản hồi từ Server
connection.on("ReceiveResponse", (agentId, data) => {
    if (agentId !== currentAgentId) return;
    handleResponse(data);
});

// Gửi lệnh
function sendCommand(action, params = {}) {
    const message = { action, ...params };
    connection.invoke("SendToAgent", currentAgentId, message);
}