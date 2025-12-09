using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RestEleven.Shared.Models;

namespace RestEleven.Client.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var timeConverter = new ValueConverter<TimeOnly, TimeSpan>(
            time => time.ToTimeSpan(),
            span => TimeOnly.FromTimeSpan(span));

        var dateConverter = new ValueConverter<DateOnly, DateTime>(
            date => date.ToDateTime(TimeOnly.MinValue),
            dateTime => DateOnly.FromDateTime(dateTime));

        var entity = modelBuilder.Entity<AttendanceEntry>();
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Date).HasConversion(dateConverter);
        entity.Property(e => e.Start).HasConversion(timeConverter);
        entity.Property(e => e.End).HasConversion(timeConverter);
        entity.Property(e => e.Comment).HasMaxLength(256);
        entity.HasIndex(e => e.Date);
        entity.HasIndex(e => new { e.Date, e.Start });
    }
}
