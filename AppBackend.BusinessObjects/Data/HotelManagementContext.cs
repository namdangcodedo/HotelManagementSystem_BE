using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Data;

public class HotelManagementContext : DbContext
{
    public HotelManagementContext() { }

    public HotelManagementContext(DbContextOptions<HotelManagementContext> options)
        : base(options) { }

    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<Amenity> Amenities { get; set; }
    public virtual DbSet<Attendance> Attendances { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<BookingService> BookingServices { get; set; }
    public virtual DbSet<CommonCode> CommonCodes { get; set; }
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
    public virtual DbSet<Feedback> Feedbacks { get; set; }
    public virtual DbSet<Holiday> Holidays { get; set; }
    public virtual DbSet<HolidayPricing> HolidayPricings { get; set; }
    public virtual DbSet<HousekeepingTask> HousekeepingTasks { get; set; }
    public virtual DbSet<Medium> Media { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Room> Rooms { get; set; }
    public virtual DbSet<RoomAmenity> RoomAmenities { get; set; }
    public virtual DbSet<Salary> Salaries { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<Transaction> Transactions { get; set; }
    public virtual DbSet<Voucher> Vouchers { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<AccountRole> AccountRoles { get; set; }
    public virtual DbSet<BookingRoom> BookingRooms { get; set; }
    public virtual DbSet<BookingRoomService> BookingRoomServices { get; set; }
    public virtual DbSet<PayrollDisbursement> PayrollDisbursements { get; set; }
    public virtual DbSet<BankConfig> BankConfigs { get; set; }
    public virtual DbSet<ChatSession> ChatSessions { get; set; }
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }
    public virtual DbSet<RoomType> RoomTypes { get; set; }
    public virtual DbSet<EmpAttendInfo> EmpAttendInfo { get; set; }
    public virtual DbSet<Comment> Comments { get; set; }




    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=103.38.236.148,1423;Database=hotel_management;User Id=sa;Password=123456789a@;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomAmenity>().HasKey(ra => new { ra.RoomId, ra.AmenityId });
        modelBuilder.Entity<CommonCode>().HasIndex(e => new { e.CodeType, e.CodeValue }).IsUnique();
        modelBuilder.Entity<Voucher>().HasIndex(v => v.Code).IsUnique();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var createdAtProp = entityType.FindProperty("CreatedAt");
            if (createdAtProp != null)
            {
                createdAtProp.SetDefaultValueSql("(getdate())");
            }
        }
        modelBuilder.Entity<AccountRole>()
            .HasKey(ar => new { ar.AccountId, ar.RoleId });
        modelBuilder.Entity<AccountRole>()
            .HasOne(ar => ar.Account)
            .WithMany(a => a.AccountRoles)
            .HasForeignKey(ar => ar.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AccountRole>()
            .HasOne(ar => ar.Role)
            .WithMany(r => r.AccountRoles)
            .HasForeignKey(ar => ar.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BookingRoom>()
            .HasOne(br => br.Booking)
            .WithMany(b => b.BookingRooms)
            .HasForeignKey(br => br.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BookingRoom>()
            .HasOne(br => br.Room)
            .WithMany(r => r.BookingRooms)
            .HasForeignKey(br => br.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BookingRoomService>()
            .HasOne(brs => brs.BookingRoom)
            .WithMany(br => br.BookingRoomServices)
            .HasForeignKey(brs => brs.BookingRoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BookingRoomService>()
            .HasOne(brs => brs.Service)
            .WithMany(s => s.BookingRoomServices)
            .HasForeignKey(brs => brs.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HolidayPricing>()
            .HasOne(hp => hp.Holiday)
            .WithMany(h => h.HolidayPricings)
            .HasForeignKey(hp => hp.HolidayId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HolidayPricing>()
            .HasOne(hp => hp.Room)
            .WithMany()
            .HasForeignKey(hp => hp.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HolidayPricing>()
            .HasOne(hp => hp.Service)
            .WithMany()
            .HasForeignKey(hp => hp.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Room>()
            .HasOne(r => r.Status)
            .WithMany()
            .HasForeignKey(r => r.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Room>()
            .HasOne(r => r.RoomType)
            .WithMany()
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Status)
            .WithMany()
            .HasForeignKey(b => b.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.BookingType)
            .WithMany()
            .HasForeignKey(b => b.BookingTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.PaymentMethod)
            .WithMany()
            .HasForeignKey(t => t.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.PaymentStatus)
            .WithMany()
            .HasForeignKey(t => t.PaymentStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.TransactionStatus)
            .WithMany()
            .HasForeignKey(t => t.TransactionStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.DepositStatus)
            .WithMany()
            .HasForeignKey(t => t.DepositStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Salary>()
            .HasOne(s => s.Employee)
            .WithMany(e => e.Salaries)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollDisbursement>()
            .HasOne(p => p.Employee)
            .WithMany(e => e.PayrollDisbursements)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollDisbursement>()
            .HasOne(p => p.Status)
            .WithMany()
            .HasForeignKey(p => p.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HousekeepingTask>()
            .HasOne(h => h.TaskType)
            .WithMany()
            .HasForeignKey(h => h.TaskTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HousekeepingTask>()
            .HasOne(h => h.Status)
            .WithMany()
            .HasForeignKey(h => h.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.FeedbackType)
            .WithMany()
            .HasForeignKey(f => f.FeedbackTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.Status)
            .WithMany()
            .HasForeignKey(f => f.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.EmployeeType)
            .WithMany()
            .HasForeignKey(e => e.EmployeeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.NotificationType)
            .WithMany()
            .HasForeignKey(n => n.NotificationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RoomType>()
               .HasMany(d => d.Rooms);

        modelBuilder.Entity<Comment>(entity =>
        {

            entity.Property(e => e.Content).HasMaxLength(640);

            entity.HasOne(d => d.Account).WithMany(p => p.Comments)
                .HasForeignKey(d => d.AccountId);

            entity.HasOne(d => d.Reply).WithMany(p => p.InverseReply)
                .HasForeignKey(d => d.ReplyId);

            entity.HasOne(d => d.RoomType).WithMany(p => p.Comments)
                .HasForeignKey(d => d.RoomTypeId);
        });
    }
}
