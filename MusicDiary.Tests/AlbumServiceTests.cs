using Microsoft.EntityFrameworkCore;
using MusicDiary.Models;
using MusicDiary.Services;

namespace MusicDiary.Tests;

public class AlbumServiceTests
{
    [Fact]
    public async Task CrudPersistsAcrossContextsAndDeleteCascades()
    {
        using var fixture = new TestDatabase();
        await fixture.Service.InitializeAsync();
        var id = await fixture.Service.SaveAsync(TestDatabase.Album());
        var saved = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Equal(id, saved.Id);
        Assert.Single(saved.Tracks); Assert.Single(saved.ListeningSessions);
        var createdAt = saved.CreatedAt;
        saved.Title = "Новая версия"; saved.Rating = 10; saved.IsFavorite = true;
        saved.Tracks[0].Note = "Другая заметка";
        saved.Tracks.Add(new Track { Title = "Финал" });
        await fixture.Service.SaveAsync(saved);
        var reloaded = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Equal("Новая версия", reloaded.Title); Assert.Equal(10, reloaded.Rating);
        Assert.Equal(createdAt, reloaded.CreatedAt); Assert.True(reloaded.IsFavorite);
        Assert.Equal(2, reloaded.Tracks.Count); Assert.Single(reloaded.ListeningSessions);
        await fixture.Service.DeleteAsync(id);
        await using var db = fixture.CreateContext();
        Assert.Empty(await db.Albums.ToListAsync()); Assert.Empty(await db.Tracks.ToListAsync());
        Assert.Empty(await db.ListeningSessions.ToListAsync());
    }

    [Fact]
    public async Task RepeatListeningKeepsHistoryAndLatestDate()
    {
        using var fixture = new TestDatabase(); await fixture.Service.InitializeAsync();
        var id = await fixture.Service.SaveAsync(TestDatabase.Album());
        await fixture.Service.AddListeningAsync(id, DateTime.Today);
        await fixture.Service.AddListeningAsync(id, DateTime.Today.AddMonths(-1));
        var album = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Equal(3, album.ListeningSessions.Count); Assert.Equal(DateTime.Today, album.ListeningDate);
        album.Review = "После повторного прослушивания";
        await fixture.Service.SaveAsync(album);
        Assert.Equal(3, Assert.Single(await fixture.Service.GetAllAsync()).ListeningSessions.Count);
        await fixture.Service.RemoveListeningAsync(album.ListeningSessions.Single(s => s.ListenedAt == DateTime.Today).Id);
        var changed = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Equal(DateTime.Today.AddDays(-2), changed.ListeningDate);
        foreach (var session in changed.ListeningSessions) await fixture.Service.RemoveListeningAsync(session.Id);
        var empty = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Null(empty.ListeningDate); Assert.Equal(AlbumStatus.WantToListen, empty.Status);
    }

    [Fact]
    public async Task CorrectingListeningDateDoesNotCreateDuplicate()
    {
        using var fixture = new TestDatabase(); await fixture.Service.InitializeAsync();
        await fixture.Service.SaveAsync(TestDatabase.Album());
        var album = Assert.Single(await fixture.Service.GetAllAsync());
        album.ListeningDate = DateTime.Today.AddDays(-3);
        await fixture.Service.SaveAsync(album);
        var changed = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Equal(DateTime.Today.AddDays(-3), Assert.Single(changed.ListeningSessions).ListenedAt);
    }

    [Fact]
    public async Task TrackChangesAreSavedWithoutChangingAlbumReview()
    {
        using var fixture = new TestDatabase(); await fixture.Service.InitializeAsync();
        var id = await fixture.Service.SaveAsync(TestDatabase.Album());
        await fixture.Service.SaveTracksAsync(id, [new Track { Title = "Другой трек", Note = "Соло", IsFavorite = true }]);
        var album = Assert.Single(await fixture.Service.GetAllAsync());
        Assert.Equal("Другой трек", Assert.Single(album.Tracks).Title);
        Assert.Equal("Соло", album.Tracks[0].Note); Assert.True(album.Tracks[0].IsFavorite);
        Assert.Equal(TestDatabase.Album().Review, album.Review);
        await fixture.Service.SaveTracksAsync(id, []);
        Assert.Empty(Assert.Single(await fixture.Service.GetAllAsync()).Tracks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void InvalidRatingIsRejected(int rating)
    {
        var album = TestDatabase.Album(rating: rating);
        Assert.Throws<ArgumentException>(() => AlbumService.Validate(album));
    }

    [Fact]
    public async Task InvalidInputDoesNotWritePartialData()
    {
        using var fixture = new TestDatabase(); await fixture.Service.InitializeAsync();
        var album = TestDatabase.Album(); album.Title = "  ";
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.SaveAsync(album));
        Assert.Empty(await fixture.Service.GetAllAsync());
        album = TestDatabase.Album(); album.ListeningDate = DateTime.Today.AddDays(1);
        Assert.Throws<ArgumentException>(() => AlbumService.Validate(album));
        album.ListeningDate = null;
        Assert.Throws<ArgumentException>(() => AlbumService.Validate(album));
        album.Status = AlbumStatus.WantToListen; album.Rating = null;
        var id = await fixture.Service.SaveAsync(album);
        Assert.True(id > 0); Assert.Empty(Assert.Single(await fixture.Service.GetAllAsync()).ListeningSessions);
    }

    [Fact]
    public async Task SqliteEnforcesRatingConstraint()
    {
        using var fixture = new TestDatabase(); await fixture.Service.InitializeAsync();
        await using var db = fixture.CreateContext();
        db.Albums.Add(TestDatabase.Album(rating: 99));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
