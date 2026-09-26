using MusicDiary.Helpers;
using MusicDiary.Models;
using MusicDiary.Services;
using MusicDiary.ViewModels;

namespace MusicDiary.Tests;

public class ViewModelTests
{
    [Fact]
    public async Task CatalogCombinesSearchFiltersAndSortsUnratedLast()
    {
        using var db = new TestDatabase(); await db.Service.InitializeAsync();
        await db.Service.SaveAsync(TestDatabase.Album("Тихий свет", "Полночь", 9));
        var other = TestDatabase.Album("Тихий берег", "Другой", 5); other.IsFavorite = true;
        await db.Service.SaveAsync(other);
        await db.Service.SaveAsync(TestDatabase.Album("Без оценки", "Полночь", null));
        var main = new MainViewModel(db.Service, new TestDialogs()); await main.InitializeAsync();
        var catalog = new CatalogViewModel(main) { TitleSearch = "ТИХИЙ", ArtistSearch = "полночь" };
        Assert.Equal("Тихий свет", Assert.Single(catalog.Albums).Title);
        catalog.SelectedRating = "5"; Assert.Empty(catalog.Albums);
        catalog.ResetCommand.Execute(null); Assert.Equal(3, catalog.Albums.Count);
        catalog.SelectedSort = "Оценка: по возрастанию";
        Assert.Equal(5, catalog.Albums[0].Rating); Assert.Null(catalog.Albums[^1].Rating);
        catalog.SelectedStatus = DisplayOptions.FilterStatuses.Single(s => s.Value == AlbumStatus.WantToListen);
        Assert.Empty(catalog.Albums);
        var favorites = new FavoritesViewModel(main);
        Assert.Equal("Тихий берег", Assert.Single(favorites.Albums).Title);
    }

    [Fact]
    public async Task EditorValidationSaveCancelAndUnsavedProtection()
    {
        using var db = new TestDatabase();
        var dialogs = new TestDialogs(); var main = new MainViewModel(db.Service, dialogs);
        await main.InitializeAsync(); main.EditAlbum(null);
        var editor = Assert.IsType<AlbumEditorViewModel>(main.CurrentPage);
        await editor.SaveCommand.ExecuteAsync(); Assert.NotEmpty(editor.ValidationMessage);
        Assert.Empty(await db.Service.GetAllAsync());
        editor.Title = "Новая запись"; editor.Artist = "Группа"; editor.Genre = "Alternative";
        Assert.True(editor.HasUnsavedChanges);
        dialogs.Answer = false; main.Navigate("Каталог"); Assert.Same(editor, main.CurrentPage);
        await editor.SaveCommand.ExecuteAsync(); Assert.IsType<AlbumDetailsViewModel>(main.CurrentPage);
        Assert.Equal("Новая запись", Assert.Single(await db.Service.GetAllAsync()).Title);
        main.EditAlbum(main.AllAlbums[0]);
        var edit = Assert.IsType<AlbumEditorViewModel>(main.CurrentPage); edit.Title = "Не сохранять";
        dialogs.Answer = true; edit.CancelCommand.Execute(null);
        Assert.Equal("Новая запись", Assert.Single(await db.Service.GetAllAsync()).Title);
    }

    [Fact]
    public void StatisticsCountUniqueAlbumsPerMonthAndIgnoreMissingRatings()
    {
        var stats = new StatisticsService(); var a = TestDatabase.Album(rating: 8); a.Id = 1;
        a.ListeningSessions = [new() { AlbumId = 1, ListenedAt = DateTime.Today }, new() { AlbumId = 1, ListenedAt = DateTime.Today }];
        var b = TestDatabase.Album(rating: null); b.Id = 2; b.Genre = "indie ROCK";
        Assert.Equal(2, stats.ListenedCount([a, b]));
        Assert.Equal(8.0.ToString("0.0"), stats.AverageRating([a, b]));
        Assert.Equal(2, Assert.Single(stats.Genres([a, b])).Count);
        Assert.Equal(1, stats.Months([a, b])[^1].Count);
        Assert.Equal(10, stats.Ratings([a, b]).Count);
        Assert.Equal("—", stats.AverageRating([]));
    }
}
