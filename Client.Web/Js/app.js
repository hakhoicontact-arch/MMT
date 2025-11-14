// =============================================
// Remote Control Client - app.js
// Tác giả: Nhóm 7 - Đồ án Mạng Máy Tính
// =============================================

// Cấu hình Server (thay đổi khi deploy)
const SERVER_URL = 'ws://localhost:8080/control'; // SignalR sẽ map tới /control
let ws = null;
let currentBinaryType = null; // 'screenshot' | 'webcam'
let binaryChunks = [];

// Kết nối WebSocket
function connectWebSocket() {
  ws = new WebSocket(SERVER_URL);

  ws.onopen = () => {
    console.log('Kết nối WebSocket thành công!');
    showStatus('Đã kết nối tới máy bị điều khiển.', 'success');
  };

  ws.onmessage = (event) => {
    // Kiểm tra nếu là binary (ảnh/video)
    if (event.data instanceof Blob) {
      handleBinaryData(event.data);
      return;
    }

    // Xử lý JSON
    try {
      const data = JSON.parse(event.data);
      handleServerMessage(data);
    } catch (e) {
      console.error('Lỗi parse JSON:', e);
    }
  };

  ws.onclose = () => {
    console.log('Mất kết nối. Thử lại sau 3s...');
    showStatus('Mất kết nối. Đang thử lại...', 'warning');
    setTimeout(connectWebSocket, 3000);
  };

  ws.onerror = (err) => {
    console.error('Lỗi WebSocket:', err);
    showStatus('Lỗi kết nối!', 'danger');
  };
}

// Gửi lệnh
function send(action, params = {}) {
  if (ws && ws.readyState === WebSocket.OPEN) {
    ws.send(JSON.stringify({ action, ...params }));
  } else {
    alert('Chưa kết nối tới máy bị điều khiển!');
  }
}

// Hiển thị trạng thái
function showStatus(msg, type = 'info') {
  const statusEl = document.getElementById('power-status');
  if (statusEl) {
    statusEl.textContent = msg;
    statusEl.className = `status ${type}`;
  }
}

// =============================================
// XỬ LÝ PHẢN HỒI TỪ SERVER
// =============================================
function handleServerMessage(data) {
  console.log('Nhận:', data);

  const { action, response, update, binary_start, binary_end, error } = data;

  if (error) {
    alert(`Lỗi: ${error}`);
    return;
  }

  // Danh sách ứng dụng
  if (action === 'app_list' && response) {
    renderApps(response);
  }

  // Danh sách tiến trình
  if (action === 'process_list' && response) {
    renderProcesses(response);
  }

  // Keylogger realtime
  if (update && currentTab() === 'keylogger') {
    const log = document.getElementById('keylog-display');
    log.textContent += update;
    log.scrollTop = log.scrollHeight;
  }

  // Xác nhận shutdown/restart
  if (response === 'done' && (action === 'shutdown' || action === 'restart')) {
    showStatus(`${action === 'shutdown' ? 'Đã gửi lệnh tắt máy' : 'Đã gửi lệnh khởi động lại'}`, 'success');
  }

  // Bắt đầu nhận ảnh/video
  if (binary_start) {
    currentBinaryType = binary_start;
    binaryChunks = [];
    console.log('Bắt đầu nhận', currentBinaryType);
  }

  // Kết thúc binary
  if (binary_end && currentBinaryType) {
    const blob = new Blob(binaryChunks, { type: 'image/jpeg' });
    const url = URL.createObjectURL(blob);

    if (currentBinaryType === 'screenshot') {
      displayScreenshot(url);
    } else if (currentBinaryType === 'webcam') {
      displayWebcamFrame(url);
    }

    binaryChunks = [];
    currentBinaryType = null;
  }
}

// Xử lý dữ liệu nhị phân (ảnh/video frame)
function handleBinaryData(blob) {
  if (currentBinaryType) {
    binaryChunks.push(blob);
  }
}

// =============================================
// TAB & UI HELPERS
// =============================================
function currentTab() {
  return document.querySelector('.tab-btn.active')?.dataset.tab;
}

// Tab switching
document.querySelectorAll('.tab-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById(btn.dataset.tab).classList.add('active');
  });
});

// =============================================
// [1] QUẢN LÝ ỨNG DỤNG
// =============================================
document.getElementById('list-apps').onclick = () => {
  send('app_list');
};

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

function startApp(name) { send('app_start', { params: { name } }); }
function stopApp(name) { send('app_stop', { params: { name } }); }

// =============================================
// [2] QUẢN LÝ TIẾN TRÌNH
// =============================================
document.getElementById('list-processes').onclick = () => send('process_list');

document.getElementById('start-process-btn').onclick = () => {
  const path = prompt('Nhập đường dẫn file thực thi (VD: notepad.exe hoặc C:\\Windows\\notepad.exe):');
  if (path?.trim()) {
    send('process_start', { params: { name: path.trim() } });
  }
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
      <td>${escapeHtml(p.name)}</td>
      <td>${p.cpu}</td>
      <td>${p.mem}</td>
      <td>
        <button class="btn danger" onclick="killProcess(${p.pid})">Dừng</button>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

function killProcess(pid) {
  if (confirm(`Dừng tiến trình PID ${pid}?`)) {
    send('process_stop', { params: { pid } });
  }
}

// =============================================
// [3] CHỤP MÀN HÌNH
// =============================================
document.getElementById('take-screenshot').onclick = () => {
  send('screenshot');
  document.getElementById('save-screenshot').style.display = 'none';
  document.getElementById('screenshot-img').src = '';
  showStatus('Đang chụp màn hình...', 'info');
};

function displayScreenshot(url) {
  const img = document.getElementById('screenshot-img');
  img.src = url;
  document.getElementById('save-screenshot').style.display = 'inline-block';
  showStatus('Đã chụp màn hình!', 'success');
}

document.getElementById('save-screenshot').onclick = () => {
  const img = document.getElementById('screenshot-img');
  const a = document.createElement('a');
  a.href = img.src;
  a.download = `screenshot_${new Date().toISOString().slice(0,19).replace(/:/g, '-')}.jpg`;
  a.click();
};

// =============================================
// [4] KEYLOGGER
// =============================================
document.getElementById('start-keylogger').onclick = () => {
  send('keylogger_start');
  toggleKeyloggerButtons(true);
  document.getElementById('keylog-display').textContent = '';
};

document.getElementById('stop-keylogger').onclick = () => {
  send('keylogger_stop');
  toggleKeyloggerButtons(false);
};

document.getElementById('clear-log').onclick = () => {
  document.getElementById('keylog-display').textContent = '';
};

function toggleKeyloggerButtons(running) {
  document.getElementById('start-keylogger').disabled = running;
  document.getElementById('stop-keylogger').disabled = !running;
}

// =============================================
// [5] TẮT / KHỞI ĐỘNG LẠI
// =============================================
document.getElementById('shutdown-btn').onclick = () => {
  if (confirm('Bạn có chắc chắn muốn TẮT máy từ xa?')) {
    send('shutdown');
  }
};

document.getElementById('restart-btn').onclick = () => {
  if (confirm('Bạn có chắc chắn muốn KHỞI ĐỘNG LẠI máy từ xa?')) {
    send('restart');
  }
};

// =============================================
// [6] WEBCAM
// =============================================
let webcamInterval = null;

document.getElementById('webcam-on').onclick = () => {
  send('webcam_on');
  document.getElementById('webcam-on').disabled = true;
  document.getElementById('webcam-off').disabled = false;
  document.getElementById('webcam-img').src = '';
  showStatus('Đang bật webcam...', 'info');
};

document.getElementById('webcam-off').onclick = () => {
  send('webcam_off');
  document.getElementById('webcam-on').disabled = false;
  document.getElementById('webcam-off').disabled = true;
  document.getElementById('webcam-img').src = '';
  showStatus('Đã tắt webcam.', 'info');
  if (webcamInterval) clearInterval(webcamInterval);
};

function displayWebcamFrame(url) {
  const img = document.getElementById('webcam-img');
  img.src = url;

  // Tự động revoke sau 100ms để giải phóng bộ nhớ
  setTimeout(() => URL.revokeObjectURL(url), 100);
}

// =============================================
// UTILS
// =============================================
function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

// Khởi động
connectWebSocket();