using System.Collections.ObjectModel;
using System.Text.Json;
using MusicDiary.Commands;
using MusicDiary.Helpers;
using MusicDiary.Models;
using MusicDiary.Services;

namespace MusicDiary.ViewModels;

public class AlbumEditorViewModel : PageViewModel
{
    private readonly int id;
    private readonly string initialState;
    private string title, artist, year, genre, rating, review;
    private string? coverPath;
    private DateTime? listeningDate;
    private AlbumStatus status;
    private bool isFavorite;
    private string validationMessage = "";
    public string Heading => id == 0 ? "Новая музыкальная история" : "Редактирование альбома";
    public string Title { get => title; set => Set(ref title, value); }
    public string Artist { get => artist; set => Set(ref artist, value); }
    public string ReleaseYear { get => year; set => Set(ref year, value); }
    public string Genre { get => genre; set => Set(ref genre, value); }
    public string Rating { get => rating; set => Set(ref rating, value); }
    public string Review { get => review; set => Set(ref review, value); }
    public string? CoverPath { get => coverPath; set => Set(ref coverPath, value); }
    public DateTime? ListeningDate { get => listeningDate; set => Set(ref listeningDate, value); }
    public AlbumStatus Status { get => status; set => Set(ref status, value); }
    public bool IsFavorite { get => isFavorite; set => Set(ref isFavorite, value); }
    public string ValidationMessage { get => validationMessage; set => Set(ref validationMessage, value); }
    public List<StatusOption> Statuses => DisplayOptions.Statuses;
    public ObservableCollection<TrackItemViewModel> Tracks { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand PickCoverCommand { get; }
    public RelayCommand RemoveCoverCommand { get; }
    public RelayCommand AddTrackCommand { get; }
    public RelayCommand RemoveTrackCommand { get; }
    public override bool HasUnsavedChanges => initialState != Snapshot();

    public AlbumEditorViewModel(MainViewModel main, Album? album) : base(main)
    {
        id = album?.Id ?? 0; title = album?.Title ?? ""; artist = album?.Artist ?? "";
        year = (album?.ReleaseYear ?? DateTime.Today.Year).ToString(); genre = album?.Genre ?? "";
        rating = album?.Rating?.ToString() ?? ""; review = album?.Review ?? "";
        coverPath = album?.CoverPath; listeningDate = album?.ListeningDate;
        status = album?.Status ?? AlbumStatus.WantToListen; isFavorite = album?.IsFavorite ?? false;
        Tracks = new((album?.Tracks ?? []).OrderBy(t => t.Id).Select(t => new TrackItemViewModel(t)));
        SaveCommand = Main.CreateCommand(_ => SaveAsync(), ex => ValidationMessage = ex is ArgumentException ? ex.Message : "Не удалось сохранить: " + ex.GetBaseException().Message);
        CancelCommand = new(_ => { if (id == 0) Main.Navigate("Каталог"); else Main.OpenAlbum(id); });
        PickCoverCommand = new(_ => { var path = Main.Dialogs.PickCover(); if (path is not null) CoverPath = path; });
        RemoveCoverCommand = new(_ => CoverPath = null);
        AddTrackCommand = new(_ => Tracks.Add(new TrackItemViewModel()));
        RemoveTrackCommand = new(p => { if (p is TrackItemViewModel track) Tracks.Remove(track); });
        initialState = Snapshot();
    }
    private string Snapshot() => JsonSerializer.Serialize(new { Title, Artist, ReleaseYear, Genre, Rating, Review, CoverPath, ListeningDate, Status, IsFavorite, Tracks = Tracks.Select(t => t.ToModel()) });
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (!int.TryParse(ReleaseYear, out var releaseYear)) throw new ArgumentException("Введите год выпуска целым числом.");
        int? score = null;
        if (!string.IsNullOrWhiteSpace(Rating))
        {
            if (!int.TryParse(Rating, out var parsed)) throw new ArgumentException("Введите оценку целым числом от 1 до 10 или оставьте поле пустым.");
            score = parsed;
        }
        var album = new Album
        {
            Id = id,
            Title = Title,
            Artist = Artist,
            ReleaseYear = releaseYear,
            Genre = Genre,
            Rating = score,
            Review = Review,
            ListeningDate = ListeningDate?.Date,
            Status = Status,
            IsFavorite = IsFavorite,
            CoverPath = CoverPath,
            Tracks = Tracks.Select(t => t.ToModel()).ToList()
        };
        AlbumService.Validate(album);
        album.CoverPath = await Main.Covers.ImportAsync(CoverPath);
        var savedId = await Main.Albums.SaveAsync(album);
        await Main.ReloadAsync(); Main.OpenAlbum(savedId, true);
        Main.Message = "Альбом сохранён.";
    }
}
