using System.Security.Cryptography;
using System.Text;
using MediSecureApi.Models;

namespace MediSecureApi.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Users.Any())
        {
            return; // Database has already been seeded
        }

        // Helper password hasher (SHA256 for simple demonstration)
        static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        // 1. Seed Users
        var admin = new User
        {
            FullName = "System Administrator",
            Email = "admin@medisecure.health",
            PasswordHash = HashPassword("AdminPass2026!"),
            Role = "Admin",
            IsAdmin = true,
            NationalIdOrSSN = "000-00-0001",
            PhoneNumber = "+1-800-555-0100",
            Address = "100 Hospital Plaza, Suite 900, San Jose, CA"
        };

        var doctor1 = new User
        {
            FullName = "Dr. Gregory House",
            Email = "dr.house@medisecure.health",
            PasswordHash = HashPassword("DoctorPass123!"),
            Role = "Doctor",
            IsAdmin = false,
            NationalIdOrSSN = "999-11-2222",
            PhoneNumber = "+1-555-019-2834",
            Address = "Diagnostics Dept, Princeton-Plainsboro Teaching Hospital"
        };

        var doctor2 = new User
        {
            FullName = "Dr. Meredith Grey",
            Email = "dr.grey@medisecure.health",
            PasswordHash = HashPassword("DoctorPass123!"),
            Role = "Doctor",
            IsAdmin = false,
            NationalIdOrSSN = "999-33-4444",
            PhoneNumber = "+1-555-019-5847",
            Address = "Surgical Wing, Grey Sloan Memorial Hospital"
        };

        var patient1 = new User
        {
            FullName = "Alice Smith",
            Email = "alice.smith@example.com",
            PasswordHash = HashPassword("PatientPass123!"),
            Role = "Patient",
            IsAdmin = false,
            NationalIdOrSSN = "987-65-4321", // Sensitive PII
            PhoneNumber = "+1-555-012-3456",
            Address = "742 Evergreen Terrace, Springfield, OR"
        };

        var patient2 = new User
        {
            FullName = "Bob Jones",
            Email = "bob.jones@example.com",
            PasswordHash = HashPassword("PatientPass123!"),
            Role = "Patient",
            IsAdmin = false,
            NationalIdOrSSN = "123-45-6789", // Sensitive PII
            PhoneNumber = "+1-555-098-7654",
            Address = "456 Elm Street, Metropolis, NY"
        };

        var patient3 = new User
        {
            FullName = "Charlie Brown",
            Email = "charlie.brown@example.com",
            PasswordHash = HashPassword("PatientPass123!"),
            Role = "Patient",
            IsAdmin = false,
            NationalIdOrSSN = "555-12-8899", // Sensitive PII
            PhoneNumber = "+1-555-043-2198",
            Address = "123 Pine St, Minneapolis, MN"
        };

        context.Users.AddRange(admin, doctor1, doctor2, patient1, patient2, patient3);
        context.SaveChanges();

        // 2. Seed Medical Records (EHR / PHI)
        var record1 = new MedicalRecord
        {
            PatientId = patient1.Id,
            PatientName = patient1.FullName,
            DoctorId = doctor1.Id,
            DoctorName = doctor1.FullName,
            Diagnosis = "Type 2 Diabetes Mellitus with Mild Neuropathy",
            Prescription = "Metformin Hydrochloride",
            Dosage = "500 mg twice daily with meals",
            ClinicalNotes = "Patient reports mild fatigue after morning workouts. Fasting blood glucose 142 mg/dL. Strict low-carb diet advised.",
            BloodType = "A-Positive",
            Allergies = "Penicillin, Sulfa drugs",
            RecordDate = DateTime.UtcNow.AddDays(-15)
        };

        var record2 = new MedicalRecord
        {
            PatientId = patient1.Id,
            PatientName = patient1.FullName,
            DoctorId = doctor2.Id,
            DoctorName = doctor2.FullName,
            Diagnosis = "Essential Hypertension, Stage 1",
            Prescription = "Lisinopril",
            Dosage = "10 mg once daily every morning",
            ClinicalNotes = "Resting blood pressure 138/88 mmHg. Follow-up consultation in 3 months.",
            BloodType = "A-Positive",
            Allergies = "Penicillin, Sulfa drugs",
            RecordDate = DateTime.UtcNow.AddDays(-45)
        };

        var record3 = new MedicalRecord
        {
            PatientId = patient2.Id,
            PatientName = patient2.FullName,
            DoctorId = doctor1.Id,
            DoctorName = doctor1.FullName,
            Diagnosis = "Chronic Obstructive Pulmonary Disease (COPD) - Moderate",
            Prescription = "Albuterol Sulfate Inhalation Aerosol",
            Dosage = "2 puffs every 4 to 6 hours as needed for shortness of breath",
            ClinicalNotes = "Spirometry indicates FEV1/FVC ratio 65%. Patient counseled on smoking cessation.",
            BloodType = "O-Negative",
            Allergies = "Latex, Aspirin",
            RecordDate = DateTime.UtcNow.AddDays(-8)
        };

        var record4 = new MedicalRecord
        {
            PatientId = patient3.Id,
            PatientName = patient3.FullName,
            DoctorId = doctor2.Id,
            DoctorName = doctor2.FullName,
            Diagnosis = "Generalized Anxiety Disorder & Mild Insomnia",
            Prescription = "Sertraline (Zoloft)",
            Dosage = "50 mg once daily at bedtime",
            ClinicalNotes = "Patient reports stress-induced sleep disruptions. Cognitive behavioral therapy recommended.",
            BloodType = "B-Positive",
            Allergies = "No known drug allergies (NKDA)",
            RecordDate = DateTime.UtcNow.AddDays(-2)
        };

        context.MedicalRecords.AddRange(record1, record2, record3, record4);

        // 3. Seed Appointments
        var appt1 = new Appointment
        {
            PatientId = patient1.Id,
            PatientName = patient1.FullName,
            DoctorId = doctor1.Id,
            DoctorName = doctor1.FullName,
            Reason = "Diabetes Quarterly Follow-up Consultation",
            AppointmentDate = DateTime.UtcNow.AddDays(3),
            Status = "Scheduled",
            ConsultationType = "Telehealth Video",
            Fee = 85.00m
        };

        var appt2 = new Appointment
        {
            PatientId = patient2.Id,
            PatientName = patient2.FullName,
            DoctorId = doctor1.Id,
            DoctorName = doctor1.FullName,
            Reason = "COPD Breathing Assessment",
            AppointmentDate = DateTime.UtcNow.AddDays(5),
            Status = "Scheduled",
            ConsultationType = "In-Clinic Visit",
            Fee = 120.00m
        };

        var appt3 = new Appointment
        {
            PatientId = patient3.Id,
            PatientName = patient3.FullName,
            DoctorId = doctor2.Id,
            DoctorName = doctor2.FullName,
            Reason = "Mental Health & Sleep Review",
            AppointmentDate = DateTime.UtcNow.AddDays(-10),
            Status = "Completed",
            ConsultationType = "Telehealth Video",
            Fee = 75.00m
        };

        context.Appointments.AddRange(appt1, appt2, appt3);

        // 4. Seed Lab Results
        var lab1 = new LabResult
        {
            PatientId = patient1.Id,
            TestName = "Comprehensive Metabolic Panel (CMP) & HbA1c",
            ResultSummary = "HbA1c: 6.8% (Target: < 7.0%). Kidney and liver function panels within normal limits.",
            OriginalFileName = "alice_smith_hba1c_lab_result.pdf",
            FilePath = "uploads/alice_smith_hba1c_lab_result.pdf",
            UploadedBy = "QuestDiagnostics_PartnerAPI"
        };

        context.LabResults.Add(lab1);

        context.SaveChanges();
    }
}
