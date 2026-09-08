# Student Quickstart & Project Guide
**MediSecure Health: Telehealth & Patient Portal Target System**  
*CS 340 Secure Software Development • Deliverable 1 (Week 4)*

---

## 1. Quick Launch Instructions

### Option A: Running with Visual Studio 2022 (Recommended)
1. Double-click **`MediSecureApi.sln`** inside the `MediSecureApi/` folder to open the solution in Visual Studio 2022.
2. In the top toolbar, ensure the startup project is set to **`MediSecureApi`**.
3. Press **`F5`** (Debug) or **`Ctrl + F5`** (Start Without Debugging).
4. Visual Studio will launch the browser automatically:
   - **Interactive Web Portal**: `http://localhost:5000` (or `https://localhost:7000`)
   - **Swagger OpenAPI Documentation**: `http://localhost:5000/swagger`

---

### Option B: Running via Terminal / Command Line
From the project root folder:

```powershell
# Restore and run the Web API project
dotnet run --project MediSecureApi/MediSecureApi/MediSecureApi.csproj
```

Once running, navigate to:
- **Interactive Portal**: `http://localhost:5000`
- **Swagger Documentation**: `http://localhost:5000/swagger`

---

## 2. Seeded User Personas & Test Credentials

The application automatically seeds a local SQLite database (`medisecure.db`) on startup with realistic clinical and administrative accounts:

| Role | Name | Email | Password | Notable Features / Data |
|:---|:---|:---|:---|:---|
| **Patient** | Alice Smith | `alice.smith@example.com` | `PatientPass123!` | Patient #4 (Type 2 Diabetes, Hypertension, Metformin prescription, SSN: 987-65-4321) |
| **Patient** | Bob Jones | `bob.jones@example.com` | `PatientPass123!` | Patient #5 (COPD, Albuterol prescription, SSN: 123-45-6789) |
| **Patient** | Charlie Brown | `charlie.brown@example.com` | `PatientPass123!` | Patient #6 (Anxiety & Insomnia, Zoloft prescription, SSN: 555-12-8899) |
| **Doctor** | Dr. Gregory House | `dr.house@medisecure.health` | `DoctorPass123!` | Doctor #2 (Chief of Diagnostic Medicine) |
| **Doctor** | Dr. Meredith Grey | `dr.grey@medisecure.health` | `DoctorPass123!` | Doctor #3 (General Surgery Attending) |
| **Admin** | System Admin | `admin@medisecure.health` | `AdminPass2026!` | Admin #1 (System Administrator, Full Access) |

---

## 3. Threat Modeling & Vulnerability Exploration Guide

The codebase is specifically structured with **9 clear vulnerability points** covering all dimensions of the **STRIDE** threat model and **OWASP API Security Top 10**:

### 🎯 Key Vulnerabilities to Inspect in Code:

1. **`TH-01: Spoofing (Weak JWT & Algorithm None)`**
   - **File**: `Services/JwtService.cs` & `Controllers/AuthController.cs`
   - **Inspection**: Observe how `ValidateToken` checks if the JWT header contains `"none"` and bypasses HMAC signature verification!
   - **Test**: Click **"Execute Token Forgery"** in the web dashboard or send an unsigned token to `/api/v1/users/diagnostics`.

2. **`TH-02: Tampering (Mass Assignment Privilege Escalation)`**
   - **File**: `Controllers/UsersController.cs` (`UpdateProfile` method) & `DTOs/UserDTOs.cs`
   - **Inspection**: Observe that `UpdateProfile` directly assigns untrusted client JSON properties (`Role`, `IsAdmin`) onto the database `User` entity.
   - **Test**: Click **"Tamper User Profile Role"** to elevate Alice from Patient to Admin.

3. **`TH-03: Tampering (Prescription & Dosage Modification)`**
   - **File**: `Controllers/RecordsController.cs` (`UpdateRecord` method)
   - **Inspection**: Note the lack of role checks and digital signatures when updating clinical prescriptions.

4. **`TH-04: Repudiation (Unlogged Deletion of Medical Records)`**
   - **File**: `Controllers/RecordsController.cs` (`DeleteRecord` method)
   - **Inspection**: Note that critical medical record deletions emit zero audit logs or database history.

5. **`TH-05: Information Disclosure (BOLA / IDOR on Medical Records)`**
   - **File**: `Controllers/RecordsController.cs` (`GetPatientRecords` method)
   - **Inspection**: Notice the missing authorization check `if (currentUser.Id != patientId && !isDoctor) return Forbid();`.
   - **Test**: Log in as Alice (Patient #4) and request `/api/v1/records/patient/5` to read Bob's private diagnosis.

6. **`TH-06: Information Disclosure (Excessive Data Exposure & Debug Secret Leaks)`**
   - **File**: `Controllers/UsersController.cs` (`GetAllUsers`, `GetDiagnostics`)
   - **Inspection**: Notice how `/api/v1/users` leaks password hashes and National ID/SSNs.

7. **`TH-07: Denial of Service (Unrestricted Resource Consumption & ReDoS)`**
   - **File**: `Controllers/RecordsController.cs` (`SearchRecords`)
   - **Inspection**: Evaluates unanchored regex queries over all records without pagination or query timeouts.

8. **`TH-08: Elevation of Privilege (BFLA on Database Export)`**
   - **File**: `Controllers/AdminController.cs` (`ExportAllHospitalData`)
   - **Inspection**: Endpoint requires authentication (`Bearer token`), but fails to verify `[Authorize(Roles = "Admin")]`. Any patient can dump the entire database!

9. **`TH-09: Security Misconfiguration / Insecure Upload (Path Traversal)`**
   - **File**: `Controllers/LabsController.cs` (`UploadLabResult`)
   - **Inspection**: Combines file path with unsanitized raw `file.FileName`, permitting directory traversal (`../../`).

---

## 4. How to Structure Your Deliverable 1 Report
Refer to the companion file **`docs/THREAT_MODELING_WORKBOOK.md`** for:
- Complete **Level 0 (Context)** and **Level 1 (Decomposition)** Data Flow Diagrams.
- Full **STRIDE Analysis Table**.
- **OWASP API 2023 & MITRE CWE** correlation matrix.
- **DREAD & Likelihood vs. Impact** risk prioritization scoring.
- Concrete **Defense-in-Depth Mitigation Strategies** (Preventative, Detective, Corrective code patches).
