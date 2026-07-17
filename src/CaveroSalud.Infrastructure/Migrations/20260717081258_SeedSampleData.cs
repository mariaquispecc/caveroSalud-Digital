using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaveroSalud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                        // Seed two patient users (passwords not set; create real users via admin UI if login needed)
                        migrationBuilder.Sql(@"
INSERT INTO ""AspNetUsers"" (""Id"", ""Dni"", ""Email"", ""EmailConfirmed"", ""FullName"", ""NormalizedEmail"", ""NormalizedUserName"", ""PhoneNumber"", ""PhoneNumberConfirmed"", ""SecurityStamp"", ""Speciality"", ""UserName"", ""IsTemporaryPassword"", ""LockoutEnabled"", ""TwoFactorEnabled"", ""AccessFailedCount"", ""FirstLoginCompleted"")
VALUES
    ('ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', '11111111', 'patient1@example.test', true, 'Paciente Uno', 'PATIENT1@EXAMPLE.TEST', 'patient1', '555-0101', true, gen_random_uuid()::text, 'General', 'patient1', false, false, false, 0, false),
    ('b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', '22222222', 'patient2@example.test', true, 'Paciente Dos', 'PATIENT2@EXAMPLE.TEST', 'patient2', '555-0102', true, gen_random_uuid()::text, 'General', 'patient2', false, false, false, 0, false) ON CONFLICT (""Id"") DO NOTHING;
");

                        // Public infos
                        migrationBuilder.Sql(@"
INSERT INTO ""PublicInfos"" (""Id"", ""Title"", ""TagLine"", ""Description"", ""Email"", ""Phone"", ""Address"") VALUES
    ('1f111111-1111-4111-8111-111111111111', 'Clinica Cavero', 'Cuidamos tu salud', 'Información pública de ejemplo', 'contacto@clinica.test', '+56900000001', 'Calle Falsa 123'),
    ('2f222222-2222-4222-8222-222222222222', 'Clinica Segunda', 'Salud y Vida', 'Otra entrada de public info', 'info@clinica2.test', '+56900000002', 'Av. Siempreviva 742') ON CONFLICT (""Id"") DO NOTHING;
");

                        // Contact messages
                        migrationBuilder.Sql(@"
INSERT INTO ""ContactMessages"" (""Id"", ""Name"", ""Email"", ""Phone"", ""Message"") VALUES
    ('3f333333-3333-4333-8333-333333333333', 'Juan Perez', 'juan@example.test', '+56911111111', 'Consulta sobre horarios'),
    ('4f444444-4444-4444-8444-444444444444', 'Maria Gomez', 'maria@example.test', '+56922222222', 'Solicitud de información') ON CONFLICT (""Id"") DO NOTHING;
");

                        // Inventory items
                        migrationBuilder.Sql(@"
INSERT INTO ""InventoryItems"" (""Id"", ""Name"", ""Unit"", ""Quantity"", ""MinThreshold"") VALUES
    ('5f555555-5555-4555-8555-555555555555', 'Paracetamol 500mg', 'tab', 100.0000, 10.0000),
    ('6f666666-6666-4666-8666-666666666666', 'Ibuprofeno 400mg', 'tab', 50.0000, 5.0000) ON CONFLICT (""Id"") DO NOTHING;
");

                        // Appointments (doctor null allowed)
                        migrationBuilder.Sql(@"
INSERT INTO ""Appointments"" (""Id"", ""PatientId"", ""Speciality"", ""StartAt"", ""EndAt"", ""Status"") VALUES
    ('7f777777-7777-4777-8777-777777777777', 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', 'General', now() + interval '1 day', now() + interval '1 day' + interval '30 minutes', 0),
    ('8f888888-8888-4888-8888-888888888888', 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', 'General', now() + interval '2 days', now() + interval '2 days' + interval '30 minutes', 0) ON CONFLICT (""Id"") DO NOTHING;
");

                        // Clinical records
                        migrationBuilder.Sql(@"
INSERT INTO ""ClinicalRecords"" (""Id"", ""AppointmentId"", ""DoctorId"", ""PatientId"", ""Diagnosis"", ""Observations"", ""Treatment"") VALUES
    ('9f999999-9999-4999-8999-999999999999', '7f777777-7777-4777-8777-777777777777', gen_random_uuid(), 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', 'Resfriado comun', 'Observaciones ejemplo', 'Reposo y analgesicos'),
    ('0f000000-0000-4000-8000-000000000000', '8f888888-8888-4888-8888-888888888888', gen_random_uuid(), 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', 'Dolor abdominal', 'Observaciones ejemplo 2', 'Dieta y seguimiento') ON CONFLICT (""Id"") DO NOTHING;
");

                        // Lab orders
                        migrationBuilder.Sql(@"
INSERT INTO ""LabOrders"" (""Id"", ""AppointmentId"", ""DoctorId"", ""PatientId"", ""TestName"", ""Status"") VALUES
    ('1a111111-1111-4111-8111-1a1111111111', '7f777777-7777-4777-8777-777777777777', gen_random_uuid(), 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', 'Hemograma completo', 'Requested'),
    ('1a222222-2222-4222-8222-1a2222222222', '8f888888-8888-4888-8888-888888888888', gen_random_uuid(), 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', 'Perfil lipídico', 'Requested') ON CONFLICT (""Id"") DO NOTHING;
");

                        // Lab results
                        migrationBuilder.Sql(@"
INSERT INTO ""LabResults"" (""Id"", ""LabOrderId"", ""Analyte"", ""Value"", ""Unit"", ""ReferenceRange"", ""Comments"", ""Published"") VALUES
    ('2a111111-1111-4111-8111-2a1111111111', '1a111111-1111-4111-8111-1a1111111111', 'Hemoglobina', '13.5', 'g/dL', '12-16', 'Normal', false),
    ('2a222222-2222-4222-8222-2a2222222222', '1a222222-2222-4222-8222-1a2222222222', 'Colesterol', '180', 'mg/dL', '<200', 'Normal', false) ON CONFLICT (""Id"") DO NOTHING;
");

                        // Prescriptions
                        migrationBuilder.Sql(@"
INSERT INTO ""Prescriptions"" (""Id"", ""AppointmentId"", ""DoctorId"", ""PatientId"", ""DeliveryNotes"", ""Status"") VALUES
    ('3a333333-3333-4333-8333-3a3333333333', '7f777777-7777-4777-8777-777777777777', gen_random_uuid(), 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', 'Entregar en farmacia', 'Requested'),
    ('3a444444-4444-4444-8444-3a4444444444', '8f888888-8888-4888-8888-888888888888', gen_random_uuid(), 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', 'Administrar con alimentos', 'Requested') ON CONFLICT (""Id"") DO NOTHING;
");

                        // Prescription items
                        migrationBuilder.Sql(@"
INSERT INTO ""PrescriptionItems"" (""Id"", ""PrescriptionId"", ""Medication"", ""Dosage"", ""Quantity"") VALUES
    ('4b444444-4444-4444-8444-4b4444444444', '3a333333-3333-4333-8333-3a3333333333', 'Paracetamol 500mg', '1 cada 8h', 10),
    ('4b555555-5555-4555-8555-4b5555555555', '3a444444-4444-4444-8444-3a4444444444', 'Ibuprofeno 400mg', '1 cada 8h', 6) ON CONFLICT (""Id"") DO NOTHING;
");

                        // Reminders
                        migrationBuilder.Sql(@"
INSERT INTO ""Reminders"" (""Id"", ""AppointmentId"", ""PatientId"", ""Message"", ""SendAt"", ""Sent"") VALUES
    ('5c555555-5555-4555-8555-5c5555555555', '7f777777-7777-4777-8777-777777777777', 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', 'Recordatorio de cita', now() + interval '12 hour', false),
    ('5c666666-6666-4666-8666-5c6666666666', '8f888888-8888-4888-8888-888888888888', 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', 'Recordatorio de preparación de examen', now() + interval '24 hour', false) ON CONFLICT (""Id"") DO NOTHING;
");

                        // User notifications
                        migrationBuilder.Sql(@"
INSERT INTO ""UserNotifications"" (""Id"", ""UserId"", ""Title"", ""Message"", ""DetailUrl"") VALUES
    ('6d666666-6666-4666-8666-6d6666666666', 'ec8b7dbd-82fb-4a08-90a0-90d2dd17e592', 'Nueva receta', 'Su receta está disponible para entrega', '/app/paciente/recetas/3a333333-3333-4333-8333-3a3333333333'),
    ('6d777777-7777-4777-8777-6d7777777777', 'b2f9c4d4-3c6f-4d6a-9e2a-3f6c5e9a8b7c', 'Recordatorio', 'Recuerde su cita', '/app/paciente/citas/8f888888-8888-4888-8888-888888888888') ON CONFLICT (""Id"") DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
