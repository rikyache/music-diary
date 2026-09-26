using System.IO;
using Microsoft.EntityFrameworkCore;
using MusicDiary.Data;
using MusicDiary.Models;
using MusicDiary.Services;

namespace MusicDiary.Tests;

public sealed class TestDatabase : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"music-diary-test-{Guid.NewGuid():N}.db");
    public DiaryDbContext CreateContext() => new(new DbContextOptionsBuilder<DiaryDbContext>()
        .UseSqlite($"Data Source={path};Pooling=False").Options);
    public AlbumService Service => new(CreateContext);
    public void Dispose()
    {
        File.Delete(path); File.Delete(path + "-shm"); File.Delete(path + "-wal");
    }
    public static Album Album(string title = "Тихий свет", string artist = "Полночь", int? rating = 8) => new()
    {
        Title = title,
        Artist = artist,
        ReleaseYear = 2024,
        Genre = "Indie rock",
        Rating = rating,
        Status = AlbumStatus.Listened,
        ListeningDate = DateTime.Today.AddDays(-2),
        Review = "Гитары, к которым хочется вернуться.",
        Tracks = [new Track { Title = "Первый снег", IsFavorite = true, Note = "Красивое вступление" }]
    };
}

public class TestDialogs : DialogService
{
    public bool Answer { get; set; } = true;
    public override bool Confirm(string text) => Answer;
    public override string? PickCover() => null;
}
