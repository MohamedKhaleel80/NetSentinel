using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetSentinel.API.Models.Entities;
using NetSentinel.API.Models.Identity;

namespace NetSentinel.API.Data;

public class NetSentinelDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<int>, int>
{
    public NetSentinelDbContext(
        DbContextOptions<NetSentinelDbContext> options)
        : base(options)
    {
    }

    // =========================
    // Application Tables
    // =========================

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<BlockedIp> BlockedIps => Set<BlockedIp>();

    public DbSet<FcmToken> FcmTokens => Set<FcmToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =====================================================
        // ApplicationUser
        // =====================================================

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName)
                .HasMaxLength(100);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });


        // =====================================================
        // Alert
        // =====================================================

        builder.Entity<Alert>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SourceIp)
                .HasMaxLength(45)
                .IsRequired();

            entity.Property(x => x.DestinationIp)
                .HasMaxLength(45)
                .IsRequired();

            entity.Property(x => x.Direction)
                .HasMaxLength(10);

            entity.Property(x => x.PacketGenerator)
                .HasMaxLength(50);

            entity.Property(x => x.SnortRule)
                .HasMaxLength(100);

            entity.Property(x => x.Action)
                .HasMaxLength(50);

            entity.Property(x => x.Message)
                .HasMaxLength(500);

            entity.Property(x => x.RawPayload)
                .HasColumnType("nvarchar(max)");

            entity.HasOne(x => x.Device)
                .WithMany(x => x.Alerts)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.OccurredAt);

            entity.HasIndex(x => x.SourceIp);

            entity.HasIndex(x => x.DestinationIp);

            entity.HasIndex(x => x.SnortRule);
        });


        // =====================================================
        // Device
        // =====================================================

        builder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(45)
                .IsRequired();

            entity.Property(x => x.MacAddress)
                .HasMaxLength(50);

            entity.Property(x => x.Hostname)
                .HasMaxLength(255);

            entity.Property(x => x.OperatingSystem)
                .HasMaxLength(100);

            entity.Property(x => x.DeviceType)
                .HasMaxLength(50);

            entity.HasIndex(x => x.IpAddress)
                .IsUnique();
        });


        // =====================================================
        // Incident
        // =====================================================

        builder.Entity<Incident>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SourceIp)
                .HasMaxLength(45)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            // Alert → Incidents
            entity.HasOne(x => x.Alert)
                .WithMany(x => x.Incidents)
                .HasForeignKey(x => x.AlertId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → Assigned Incidents
            entity.HasOne(x => x.AssignedToUser)
                .WithMany(x => x.AssignedIncidents)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.Status);

            entity.HasIndex(x => x.SourceIp);
        });


        // =====================================================
        // BlockedIp
        // =====================================================

        builder.Entity<BlockedIp>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(45)
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasMaxLength(500);

            // Incident → BlockedIps
            entity.HasOne(x => x.Incident)
                .WithMany(x => x.BlockedIps)
                .HasForeignKey(x => x.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → Requested Blocks
            entity.HasOne(x => x.RequestedByUser)
                .WithMany(x => x.RequestedBlocks)
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.IpAddress);
        });


        // =====================================================
        // RefreshToken
        // =====================================================

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.Token)
                .IsUnique();

            entity.HasIndex(x => x.ExpiresAt);
        });


        // =====================================================
        // FCM Token
        // =====================================================

        builder.Entity<FcmToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.DeviceIdentifier)
                .HasMaxLength(200);

            entity.Property(x => x.Platform)
                .HasMaxLength(50);

            entity.HasOne(x => x.User)
                .WithMany(x => x.FcmTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.Token)
                .IsUnique();
        });


        // =====================================================
        // AuditLog
        // =====================================================

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityId)
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(45);

            entity.HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.CreatedAt);

            entity.HasIndex(x => x.UserId);
        });
    }
}