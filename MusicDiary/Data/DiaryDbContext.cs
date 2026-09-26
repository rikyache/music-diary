using System.IO;
using Microsoft.EntityFrameworkCore;
using MusicDiary.Models;

namespace MusicDiary.Data;

public class DiaryDbContext : DbContext
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicDiary");
    public DiaryDbContext() { }
    public DiaryDbContext(DbContextOptions<DiaryDbContext> options) : base(options) { }
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<ListeningSession> ListeningSessions => Set<ListeningSession>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (options.IsConfigured) return;
        Directory.CreateDirectory(DataDirectory);
        options.UseSqlite($"Data Source={Path.Combine(DataDirectory, "diary.db")}");
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var album = model.Entity<Album>();
        album.Property(a => a.Title).HasMaxLength(200).IsRequired();
        album.Property(a => a.Artist).HasMaxLength(200).IsRequired();
        album.Property(a => a.Genre).HasMaxLength(80).IsRequired();
        album.HasIndex(a => a.CreatedAt);
        album.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Album_Rating", "Rating IS NULL OR Rating BETWEEN 1 AND 10");
            t.HasCheckConstraint("CK_Album_Title", "length(trim(Title)) BETWEEN 1 AND 200");
            t.HasCheckConstraint("CK_Album_Artist", "length(trim(Artist)) BETWEEN 1 AND 200");
            t.HasCheckConstraint("CK_Album_Year", "ReleaseYear BETWEEN 1000 AND 9999");
            t.HasCheckConstraint("CK_Album_Status", "Status IN (0, 1, 2)");
        });
        album.HasMany(a => a.Tracks).WithOne().HasForeignKey(t => t.AlbumId).OnDelete(DeleteBehavior.Cascade);
        album.HasMany(a => a.ListeningSessions).WithOne().HasForeignKey(s => s.AlbumId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Track>().Property(t => t.Title).HasMaxLength(200).IsRequired();
        model.Entity<Track>().ToTable(t => t.HasCheckConstraint("CK_Track_Title", "length(trim(Title)) BETWEEN 1 AND 200"));
        model.Entity<ListeningSession>().HasIndex(s => s.ListenedAt);
    }
}
