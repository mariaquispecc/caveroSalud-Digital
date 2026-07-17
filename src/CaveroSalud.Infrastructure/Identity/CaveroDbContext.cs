using System;
using CaveroSalud.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CaveroSalud.Infrastructure.Identity
{
    public class CaveroDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public CaveroDbContext(DbContextOptions<CaveroDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<ClinicalRecord> ClinicalRecords { get; set; }
        public DbSet<LabOrder> LabOrders { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<LabResult> LabResults { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Speciality> Specialities { get; set; }
        public DbSet<PublicInfo> PublicInfos { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Map additional properties
            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.FullName).HasMaxLength(200);
                b.Property(u => u.Dni).HasMaxLength(20);
                b.Property(u => u.Speciality).HasMaxLength(200);
            });

            builder.Entity<ContactMessage>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Name).HasMaxLength(200).IsRequired();
                b.Property(c => c.Email).HasMaxLength(200).IsRequired();
                b.Property(c => c.Message).HasMaxLength(2000).IsRequired();
                b.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            });

            builder.Entity<Appointment>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.Speciality).HasMaxLength(200).IsRequired();
                b.Property(a => a.StartAt).IsRequired();
                b.Property(a => a.EndAt).IsRequired();
                b.Property(a => a.Status)
                    .HasConversion<int>()
                    .HasDefaultValue(AppointmentStatus.Scheduled);
            });

            builder.Entity<DoctorAvailability>(b =>
            {
                b.HasKey(d => d.Id);
                b.Property(d => d.StartAt).IsRequired();
                b.Property(d => d.EndAt).IsRequired();
            });

            builder.Entity<Reminder>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.SendAt).IsRequired();
                b.Property(r => r.Sent).HasDefaultValue(false);
                b.Property(r => r.Message).HasMaxLength(1000);
            });

            builder.Entity<ClinicalRecord>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Diagnosis).HasMaxLength(2000);
                b.Property(c => c.Treatment).HasMaxLength(2000);
                b.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
                b.Property(c => c.Observations).HasMaxLength(2000);
                b.Property(c => c.IsClosed).HasDefaultValue(false);
            });

            builder.Entity<LabOrder>(b =>
            {
                b.HasKey(l => l.Id);
                b.Property(l => l.TestName).HasMaxLength(500);
                b.Property(l => l.Status).HasMaxLength(100);
                b.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
            });

            builder.Entity<Prescription>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
                b.Property(p => p.Status).HasMaxLength(100).HasDefaultValue("Requested");
                b.Property(p => p.DeliveredById);
                b.Property(p => p.DeliveredAt);
                b.Property(p => p.DeliveryNotes).HasMaxLength(1000);
                b.HasMany(p => p.Items)
                    .WithOne(i => i.Prescription)
                    .HasForeignKey(i => i.PrescriptionId)
                    .IsRequired();
            });

            builder.Entity<PrescriptionItem>(b =>
            {
                b.HasKey(i => i.Id);
                b.Property(i => i.Medication).HasMaxLength(500);
                b.Property(i => i.Dosage).HasMaxLength(200);
            });

            builder.Entity<LabResult>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Analyte).HasMaxLength(200).IsRequired();
                b.Property(r => r.Value).HasMaxLength(200);
                b.Property(r => r.Unit).HasMaxLength(50);
                b.Property(r => r.ReferenceRange).HasMaxLength(200);
                b.Property(r => r.Comments).HasMaxLength(1000);
                b.Property(r => r.Published).HasDefaultValue(false);
                b.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
            });

            builder.Entity<InventoryItem>(b =>
            {
                b.HasKey(i => i.Id);
                b.Property(i => i.Name).HasMaxLength(500).IsRequired();
                b.Property(i => i.Unit).HasMaxLength(50);
                b.Property(i => i.Quantity).HasColumnType("numeric(18,4)");
                b.Property(i => i.MinThreshold).HasColumnType("numeric(18,4)");
            });

            builder.Entity<Speciality>(b =>
            {
                b.HasKey(s => s.Id);
                b.Property(s => s.Name).HasMaxLength(200).IsRequired();
                b.Property(s => s.Description).HasMaxLength(2000);
                b.Property(s => s.IsActive).HasDefaultValue(true);
            });

            builder.Entity<PublicInfo>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Title).HasMaxLength(200).IsRequired();
                b.Property(p => p.TagLine).HasMaxLength(400);
                b.Property(p => p.Description).HasMaxLength(4000);
                b.Property(p => p.Address).HasMaxLength(500);
                b.Property(p => p.Email).HasMaxLength(200);
                b.Property(p => p.Phone).HasMaxLength(50);
                b.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");
            });

            builder.Entity<UserNotification>(b =>
            {
                b.HasKey(n => n.Id);
                b.Property(n => n.Title).HasMaxLength(200).IsRequired();
                b.Property(n => n.Message).HasMaxLength(2000).IsRequired();
                b.Property(n => n.Type).HasMaxLength(50).HasDefaultValue("info");
                b.Property(n => n.SourceKey).HasMaxLength(200);
                b.Property(n => n.DetailUrl).HasMaxLength(500);
                b.Property(n => n.IsRead).HasDefaultValue(false);
                b.Property(n => n.CreatedAt).HasDefaultValueSql("now()");
                b.HasIndex(n => new { n.UserId, n.IsRead });
                b.HasIndex(n => n.SourceKey);
            });
        }
    }
}
