using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class IOTShowroomContext : DbContext
{
    public IOTShowroomContext()
    {
    }

    public IOTShowroomContext(DbContextOptions<IOTShowroomContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassEnrollment> ClassEnrollments { get; set; }

    public virtual DbSet<Evaluation> Evaluations { get; set; }

    public virtual DbSet<EvaluationDetail> EvaluationDetails { get; set; }

    public virtual DbSet<HallOfFame> HallOfFames { get; set; }

    public virtual DbSet<LiveDemo> LiveDemos { get; set; }

    public virtual DbSet<LiveDemoSensor> LiveDemoSensors { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectApprovalHistory> ProjectApprovalHistories { get; set; }

    public virtual DbSet<ProjectAsset> ProjectAssets { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<ProjectMilestone> ProjectMilestones { get; set; }

    public virtual DbSet<ProjectSubmission> ProjectSubmissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Rubric> Rubrics { get; set; }

    public virtual DbSet<RubricWeight> RubricWeights { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorDatum> SensorData { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=localhost,1433;Initial Catalog=IOTShowroom;Persist Security Info=True;User ID=sa;Password=123456789a@;Encrypt=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__C640A82D80ABE06F");

            entity.HasOne(d => d.Admin).WithMany(p => p.Announcements).HasConstraintName("FK_Announcements_Admin");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__FDF47986C6BBE5E5");

            entity.HasOne(d => d.Instructor).WithMany(p => p.Classes).HasConstraintName("FK_Classes_Instructor");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes).HasConstraintName("FK_Classes_Semester");
        });

        modelBuilder.Entity<ClassEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Class_En__6D24AA7A09B7DB62");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassEnrollments).HasConstraintName("FK_Enrollments_Class");

            entity.HasOne(d => d.Student).WithMany(p => p.ClassEnrollments).HasConstraintName("FK_Enrollments_Student");
        });

        modelBuilder.Entity<Evaluation>(entity =>
        {
            entity.HasKey(e => e.EvaluationId).HasName("PK__Evaluati__827C592D4FC39243");

            entity.HasOne(d => d.Instructor).WithMany(p => p.Evaluations).HasConstraintName("FK_Evaluations_Instructor");

            entity.HasOne(d => d.Project).WithMany(p => p.Evaluations).HasConstraintName("FK_Evaluations_Project");
        });

        modelBuilder.Entity<EvaluationDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__Evaluati__38E9A224263B5411");

            entity.HasOne(d => d.Evaluation).WithMany(p => p.EvaluationDetails).HasConstraintName("FK_EvalDetails_Evaluation");

            entity.HasOne(d => d.Rubric).WithMany(p => p.EvaluationDetails).HasConstraintName("FK_EvalDetails_Rubric");
        });

        modelBuilder.Entity<HallOfFame>(entity =>
        {
            entity.HasKey(e => e.HofId).HasName("PK__Hall_of___A7FA0EFE9BDCBBC9");

            entity.HasOne(d => d.Project).WithMany(p => p.HallOfFames).HasConstraintName("FK_HOF_Project");

            entity.HasOne(d => d.Semester).WithMany(p => p.HallOfFames).HasConstraintName("FK_HOF_Semester");
        });

        modelBuilder.Entity<LiveDemo>(entity =>
        {
            entity.HasKey(e => e.DemoId).HasName("PK__Live_Dem__A77EA3F0ACFB453A");

            entity.HasOne(d => d.Project).WithMany(p => p.LiveDemos).HasConstraintName("FK_LiveDemo_Project");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemos).HasConstraintName("FK_LiveDemo_Sensor");
        });

        modelBuilder.Entity<LiveDemoSensor>(entity =>
        {
            entity.HasKey(e => e.LdsId).HasName("PK__Live_Dem__A3A7250B504BC41A");

            entity.HasOne(d => d.Demo).WithMany(p => p.LiveDemoSensors).HasConstraintName("FK_LDS_Demo");

            entity.HasOne(d => d.Sensor).WithMany(p => p.LiveDemoSensors).HasConstraintName("FK_LDS_Sensor");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__0BBF6EE61FA26B94");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers).HasConstraintName("FK_Messages_Receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders).HasConstraintName("FK_Messages_Sender");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842F65AEF100");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__BC799E1F4515E28E");

            entity.HasOne(d => d.Class).WithMany(p => p.Projects).HasConstraintName("FK_Projects_Class");

            entity.HasOne(d => d.Leader).WithMany(p => p.Projects).HasConstraintName("FK_Projects_Leader");
        });

        modelBuilder.Entity<ProjectApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Project___096AA2E9E3530770");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ProjectApprovalHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Reviewer");

            entity.HasOne(d => d.Submission).WithMany(p => p.ProjectApprovalHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApprovalHistory_Submission");
        });

        modelBuilder.Entity<ProjectAsset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__Project___D28B561D5D4D7819");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectAssets).HasConstraintName("FK_Project_Assets_Project");
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Project___B29B85342BF23F4F");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers).HasConstraintName("FK_Project_Members_Project");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectMembers).HasConstraintName("FK_Project_Members_User");
        });

        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneId).HasName("PK__Project___67592EB7BCF1E706");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMilestones)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Milestones_Project");
        });

        modelBuilder.Entity<ProjectSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("PK__Project___9B53559522FF4404");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectSubmissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submissions_Project");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.ProjectSubmissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Submissions_User");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CCE3CEA0C6");
        });

        modelBuilder.Entity<Rubric>(entity =>
        {
            entity.HasKey(e => e.RubricId).HasName("PK__Rubrics__A1FB3B3A54CA5B29");
        });

        modelBuilder.Entity<RubricWeight>(entity =>
        {
            entity.HasKey(e => e.WeightId).HasName("PK__Rubric_W__453932ACE56F44E9");

            entity.HasOne(d => d.Class).WithMany(p => p.RubricWeights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Class");

            entity.HasOne(d => d.Rubric).WithMany(p => p.RubricWeights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RubricWeights_Rubric");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__CBC81B01BEC31556");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("PK__sensors__1A8E906028A273BE");

            entity.HasOne(d => d.Project).WithMany(p => p.Sensors).HasConstraintName("FK_sensors_Project");
        });

        modelBuilder.Entity<SensorDatum>(entity =>
        {
            entity.HasKey(e => e.DataId).HasName("PK__sensor_d__F5A76B3B81CAD6A5");

            entity.HasOne(d => d.Sensor).WithMany(p => p.SensorData).HasConstraintName("FK_sensor_data_Sensor");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F77954EFA");

            entity.HasOne(d => d.Role).WithMany(p => p.Users).HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
