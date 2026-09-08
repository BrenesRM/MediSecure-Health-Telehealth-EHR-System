# MediSecure Health — Telehealth & EHR System
## CS 340 Secure Software Development • Deliverable 1 (Week 4) Target Application

**MediSecure Health** is a modern, microservice-ready Telehealth & Electronic Health Records (EHR) Web API built with **C# and .NET 8**. It is intentionally designed as an educational reference target for **Threat Modeling and Architectural Vulnerability Analysis (STRIDE, DFD Level 0/1, OWASP API Top 10, CWE, DREAD Risk Scoring)**.

---

## 📋 Table of Contents
1. [Prerequisites](#1-prerequisites)
2. [Project Architecture & File Layout](#2-project-architecture--file-layout)
3. [Step-by-Step Local Setup & Configuration](#3-step-by-step-local-setup--configuration)
   - [Method A: Visual Studio 2022 (Recommended)](#method-a-visual-studio-2022-recommended)
   - [Method B: .NET CLI / PowerShell / Terminal](#method-b-net-cli--powershell--terminal)
   - [Method C: Visual Studio Code](#method-c-visual-studio-code)
4. [Using the Application & Seeded Accounts](#4-using-the-application--seeded-accounts)
5. [Step-by-Step STRIDE Vulnerability Testing Guide](#5-step-by-step-stride-vulnerability-testing-guide)
6. [Resetting the Database](#6-resetting-the-database)
7. [Academic Report & Deliverable 1 Reference Guide](#7-academic-report--deliverable-1-reference-guide)

---

## 1. Prerequisites

Before running the application on your local machine, ensure you have the following installed:

- **Operating System**: Windows 10/11, macOS, or Linux.
- **.NET 8.0 SDK**: [Download .NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  - Verify installation in your terminal:
    ```powershell
    dotnet --version
    # Expected output: 8.0.xxx (e.g., 8.0.424)
    ```
- **IDE (Choose one)**:
  - **Visual Studio 2022** (Community, Professional, or Enterprise) with the **"ASP.NET and web development"** workload selected.
  - **Visual Studio Code** with the **C# Dev Kit** extension.
- **Web Browser**: Microsoft Edge, Google Chrome, Mozilla Firefox, or Safari.

---

## 2. Project Architecture & File Layout

```
Proyect_1/
├── MediSecureApi/
│   ├── MediSecureApi.sln                    # Visual Studio 2022 Solution
│   ├── nuget.config                         # NuGet package source configuration
│   ├── MediSecureApi/
│   │   ├── MediSecureApi.csproj             # .NET 8 Web API Project File
│   │   ├── Program.cs                       # App entry point, middleware, DI, Swagger, Seed
│   │   ├── appsettings.json                 # Connection strings, JWT settings, debug flags
│   │   ├── Models/                          # Domain models (User, MedicalRecord, Appointment, LabResult)
│   │   ├── DTOs/                            # Data Transfer Objects (Auth, Users, Records, Appointments)
│   │   ├── Data/                            # EF Core SQLite DbContext & automatic DbInitializer
│   │   ├── Services/                        # JwtService (with TH-01 algorithm 'none' flaw)
│   │   ├── Controllers/                     # API Controllers (Auth, Users, Records, Admin, Labs, Webhooks)
│   │   ├── uploads/                         # Storage directory for diagnostic lab files
│   │   └── wwwroot/                         # Interactive Dark-Mode Web Dashboard & STRIDE Demonstrator
│   │       ├── index.html                   # Single-Page Application Portal
│   │       ├── style.css                    # Modern glassmorphic styles
│   │       └── app.js                       # Frontend client logic & live API inspector
│   └── README.md                            # Solution quickstart
├── docs/
│   ├── THREAT_MODELING_WORKBOOK.md          # Complete reference workbook (DFD 0/1, STRIDE, DREAD, Mitigations)
│   └── STUDENT_QUICKSTART.md                # 2-minute student guide for Visual Studio
└── README.md                                # This local configuration guide
```

---

## 3. Step-by-Step Local Setup & Configuration

### Method A: Visual Studio 2022 (Recommended)

1. **Open the Solution**:
   - Open Visual Studio 2022.
   - Click **Open a project or solution**.
   - Navigate to `Proyect_1/MediSecureApi/` and select **`MediSecureApi.sln`**.

2. **Verify Startup Project**:
   - In the **Solution Explorer** panel (right side), confirm that **`MediSecureApi`** is bold (set as Startup Project). If not, right-click `MediSecureApi` and select **Set as Startup Project**.

3. **Build the Solution**:
   - In the top menu, click **Build** → **Build Solution** (or press `Ctrl + Shift + B`).
   - The Output window will report: `Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped`.

4. **Launch & Debug**:
   - Press **`F5`** (Debug Mode) or **`Ctrl + F5`** (Start Without Debugging).
   - Visual Studio automatically launches your default browser at:
     - **Interactive Web Portal**: `http://localhost:5000` (or `https://localhost:7000`)
     - **Swagger OpenAPI Documentation**: `http://localhost:5000/swagger`

---

### Method B: .NET CLI / PowerShell / Terminal

1. **Open your Terminal / PowerShell** and navigate to the project directory:
   ```powershell
   cd "d:\TrabajoBCCR\Personal\Servicios-de-Profesor\ULACIT\CS 340 Secure Software Development\Class Material\Proyects\Proyect_1"
   ```

2. **Restore Dependencies**:
   ```powershell
   dotnet restore MediSecureApi/MediSecureApi/MediSecureApi.csproj
   ```

3. **Build the Project**:
   ```powershell
   dotnet build MediSecureApi/MediSecureApi/MediSecureApi.csproj
   ```

4. **Run the Application**:
   ```powershell
   dotnet run --project MediSecureApi/MediSecureApi/MediSecureApi.csproj
   ```

5. **Open in Browser**:
   - Navigate to **`http://localhost:5000`** for the Interactive Web Portal.
   - Navigate to **`http://localhost:5000/swagger`** for the OpenAPI Swagger testing interface.

---

### Method C: Visual Studio Code

1. Open VS Code: `File` → `Open Folder...` → Select `Proyect_1/MediSecureApi`.
2. When prompted by the C# Dev Kit, select **`MediSecureApi.sln`** as the active solution.
3. Open the integrated terminal (`Ctrl + ~`) and type:
   ```powershell
   dotnet run --project MediSecureApi/MediSecureApi.csproj
   ```
4. Open your browser at `http://localhost:5000`.

---

## 4. Using the Application & Seeded Accounts

The application uses an embedded **SQLite Database** (`medisecure.db`) that is automatically created and seeded on startup. No separate database server installation (SQL Server/Postgres) is required!

### Pre-configured User Accounts:

| Persona / Role | Full Name | Email | Password | Pre-seeded Records & Role |
|:---|:---|:---|:---|:---|
| **Patient #4** | Alice Smith | `alice.smith@example.com` | `PatientPass123!` | Has Type 2 Diabetes & Hypertension records, Metformin & Lisinopril prescriptions. |
| **Patient #5** | Bob Jones | `bob.jones@example.com` | `PatientPass123!` | Has COPD records, Albuterol inhaler prescription. |
| **Patient #6** | Charlie Brown | `charlie.brown@example.com` | `PatientPass123!` | Has Anxiety & Insomnia records, Zoloft prescription. |
| **Doctor #2** | Dr. Gregory House | `dr.house@medisecure.health` | `DoctorPass123!` | Chief of Diagnostic Medicine. |
| **Doctor #3** | Dr. Meredith Grey | `dr.grey@medisecure.health` | `DoctorPass123!` | General Surgery Attending. |
| **Admin #1** | System Administrator | `admin@medisecure.health` | `AdminPass2026!` | Full Hospital System Administrator. |

---

## 5. Step-by-Step STRIDE Vulnerability Testing Guide

You can test every vulnerability either through the **Interactive Web UI** (`http://localhost:5000`) or via **Swagger UI** (`http://localhost:5000/swagger`).

### 🕵️‍♂️ TH-01: Spoofing (Weak JWT / Algorithm "none")
- **Flaw Location**: `Services/JwtService.cs` & `Controllers/AuthController.cs`
- **Concept**: The custom JWT validator accepts tokens signed with algorithm `"none"` and skips cryptographic HMAC signature verification.
- **How to Test**:
  1. In the Web UI, click the **"⚡ Execute Token Forgery"** button.
  2. The frontend calls `/api/v1/auth/forge-token` with `useNoneAlgorithm: true`.
  3. The forged unsigned token is sent to `/api/v1/users/diagnostics?debug=true`.
  4. Notice that the server accepts the forged token and returns internal server diagnostics, database connection strings, and the secret key!

---

### 🕵️‍♂️ TH-02: Tampering (Mass Assignment Privilege Escalation)
- **Flaw Location**: `Controllers/UsersController.cs` (`UpdateProfile` method) & `DTOs/UserDTOs.cs`
- **Concept**: The profile update endpoint binds untrusted client JSON fields (`Role`, `IsAdmin`) directly onto the database `User` entity.
- **How to Test**:
  1. Log in as **Alice Smith (Patient)**.
  2. Click **"⚡ Tamper User Profile Role"** in the Web UI, or execute via Swagger:
     - `PUT /api/v1/users/profile` with payload:
       ```json
       {
         "Role": "Admin",
         "IsAdmin": true
       }
       ```
  3. Notice that Alice's role changes to `Admin` and `IsAdmin: true`.

---

### 🕵️‍♂️ TH-03: Tampering (Prescription & Dosage Modification)
- **Flaw Location**: `Controllers/RecordsController.cs` (`UpdateRecord` method)
- **Concept**: Patients can directly modify medical dosages and doctor diagnoses on record entries without physician authorization.
- **How to Test via Swagger**:
  1. Send `PUT /api/v1/records/1` with payload:
     ```json
     {
       "prescription": "Metformin Extra Strength",
       "dosage": "5000 mg (Lethal Overdose)"
     }
     ```
  2. Observe that the clinical record is overwritten with no integrity signature validation.

---

### 🕵️‍♂️ TH-04: Repudiation (Unlogged Medical Record Deletions)
- **Flaw Location**: `Controllers/RecordsController.cs` (`DeleteRecord` method)
- **Concept**: Critical clinical records can be permanently deleted without writing an entry to a persistent audit trail.
- **How to Test via Swagger**:
  1. Send `DELETE /api/v1/records/2`.
  2. The record is permanently removed from `medisecure.db` with zero audit log generation.

---

### 🕵️‍♂️ TH-05: Information Disclosure (BOLA / IDOR Cross-Patient Records)
- **Flaw Location**: `Controllers/RecordsController.cs` (`GetPatientRecords` method)
- **Concept**: `/api/v1/records/patient/{patientId}` lacks an object-ownership authorization check.
- **How to Test**:
  1. Log in as **Alice Smith (Patient #4)**.
  2. Click **"⚡ Query Cross-Patient Records"** in the Web UI, or send `GET /api/v1/records/patient/5` in Swagger.
  3. Notice that Alice can view **Bob Jones's** private diagnosis (COPD, prescriptions, clinical notes).

---

### 🕵️‍♂️ TH-06: Information Disclosure (Excessive Data Exposure & SSN Leaks)
- **Flaw Location**: `Controllers/UsersController.cs` (`GetAllUsers` method)
- **Concept**: User listing endpoint serializes the full User entity, returning National ID / SSNs and password hashes in plaintext.
- **How to Test**:
  1. Click **"⚡ Leak User PII & Credential Hashes"** in the Web UI, or send `GET /api/v1/users`.
  2. Inspect the response to see all users' `NationalIdOrSSN` and `PasswordHash`.

---

### 🕵️‍♂️ TH-07: Denial of Service (ReDoS & Unthrottled Login)
- **Flaw Location**: `Controllers/RecordsController.cs` (`SearchRecords` method)
- **Concept**: Evaluates user-supplied unanchored regular expressions in memory across all database records with no timeout, pagination, or rate limits.
- **How to Test via Swagger**:
  - `GET /api/v1/records/search?query=((a%2B)%2B)%2B$` (Catastrophic backtracking ReDoS query).

---

### 🕵️‍♂️ TH-08: Elevation of Privilege (BFLA Full Database Dump)
- **Flaw Location**: `Controllers/AdminController.cs` (`ExportAllHospitalData` method)
- **Concept**: Administrative export endpoint checks if the user has a valid token, but omits the role verification (`Role == "Admin"`).
- **How to Test**:
  1. Log in as a standard **Patient** (Alice or Bob).
  2. Click **"⚡ Trigger Admin Data Dump"** in the Web UI, or call `GET /api/v1/admin/export-database`.
  3. Notice that the patient token dumps the entire hospital database (all users, records, appointments, and lab files).

---

### 🕵️‍♂️ TH-09: Misconfiguration & Tampering (Insecure Upload & Path Traversal)
- **Flaw Location**: `Controllers/LabsController.cs` (`UploadLabResult` method)
- **Concept**: Accepts arbitrary file extensions and uses raw `file.FileName` concatenated with the upload path (`../../`).
- **How to Test via Swagger**:
  - Send `POST /api/v1/labs/upload` with a multipart file named `../../test_override.txt`.

---

## 6. Resetting the Database

If you modify or delete records during testing and want to reset the database to its clean initial state:

1. Stop the running application (`Ctrl + C` in the terminal, or stop debugging in Visual Studio).
2. Delete the **`medisecure.db`** file in `MediSecureApi/MediSecureApi/`.
3. Restart the application. EF Core will automatically recreate `medisecure.db` and re-seed all default accounts, records, and appointments.

---

## 7. Academic Report & Deliverable 1 Reference Guide

This project is tailored specifically to satisfy all criteria of the **CS 340 Deliverable 1 Evaluation Rubric (100 Points Total / 15% Course Grade)**:

| Rubric Requirement | Where to Find in this Repository |
|:---|:---|
| **System Modeling & DFD (20%)** | See **`docs/THREAT_MODELING_WORKBOOK.md` (Section 3)** for Level 0 (Context) and Level 1 (Decomposition) Mermaid DFDs with explicit Trust Boundaries. |
| **Attack Surface & Assets (15%)** | See **`docs/THREAT_MODELING_WORKBOOK.md` (Sections 2 & 4)** for Asset Catalog with Sensitivity Labels and Full Endpoint Matrix. |
| **STRIDE Threat Modeling (25%)** | See **`docs/THREAT_MODELING_WORKBOOK.md` (Section 5)** for detailed scenarios across all 6 STRIDE dimensions. |
| **Vulnerability & Risk Scoring (20%)** | See **`docs/THREAT_MODELING_WORKBOOK.md` (Sections 6 & 7)** for OWASP API 2023 / CWE mapping and DREAD / Likelihood vs. Impact scoring tables. |
| **Mitigation & Security Controls (15%)** | See **`docs/THREAT_MODELING_WORKBOOK.md` (Section 8)** for Preventative, Detective, and Corrective C# code patches and architectural controls. |
| **Technical Quality & Style (5%)** | See **`docs/STUDENT_QUICKSTART.md`** for report writing tips and IEEE formatting checklists. |

---

**Course**: CS 340 Secure Software Development • Universidad Latinoamericana de Ciencia y Tecnología (ULACIT)  
**Instructor / Author**: Marlon Esteban Brenes Rojas
