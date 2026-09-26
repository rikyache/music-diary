using System.Collections.ObjectModel;
using System.Text.Json;
using MusicDiary.Commands;
using MusicDiary.Models;

namespace MusicDiary.ViewModels;

public class AlbumDetailsViewModel : PageViewModel
{
    public Album Album { get; }
    public ObservableCollection<TrackItemViewModel> Tracks { get; }
    public List<ListeningSession> History => Album.ListeningSessions.OrderByDescending(s => s.ListenedAt).ThenByDescending(s => s.Id).ToList();
    public bool NoHistory => History.Count == 0;
    public string FavoriteLabel => Album.IsFavorite ? "♥  В избранном" : "♡  В избранное";
    public string ReviewText => string.IsNullOrWhiteSpace(Album.Review) ? "Пока без отзыва. Сохраните первое впечатление через редактирование." : Album.Review;
    private string initialTracks;
    private DateTime? newListeningDate = DateTime.Today;
    public DateTime? NewListeningDate { get => newListeningDate; set => Set(ref newListeningDate, value); }
    public override bool HasUnsavedChanges => initialTracks != Snapshot();
    public RelayCommand EditCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand FavoriteCommand { get; }
    public AsyncRelayCommand AddListeningCommand { get; }
    public AsyncRelayCommand RemoveListeningCommand { get; }
    public RelayCommand AddTrackCommand { get; }
    public RelayCommand RemoveTrackCommand { get; }
    public AsyncRelayCommand SaveTracksCommand { get; }

    public AlbumDetailsViewModel(MainViewModel main, Album album) : base(main)
    {
        Album = album;
        Tracks = new(album.Tracks.OrderBy(t => t.Id).Select(t => new TrackItemViewModel(t)));
        initialTracks = Snapshot();
        EditCommand = new(_ => Main.EditAlbum(Album));
        DeleteCommand = Main.CreateCommand(async _ =>
        {
            if (!Main.Dialogs.Confirm($"Удалить «{Album.Title}» вместе с треками и историей прослушиваний?")) return;
            await Main.Albums.DeleteAsync(Album.Id); await Main.ReloadAsync(); Main.Navigate("Каталог", true); Main.Message = "Альбом удалён.";
        }, Main.ReportError);
        FavoriteCommand = Main.CreateCommand(async _ =>
        {
            await Main.Albums.ToggleFavoriteAsync(Album.Id); Album.IsFavorite = !Album.IsFavorite;
            Raise(nameof(FavoriteLabel)); await Main.ReloadAsync();
        }, Main.ReportError);
        AddListeningCommand = Main.CreateCommand(async _ =>
        {
            if (NewListeningDate is null) throw new ArgumentException("Выберите дату прослушивания.");
            await Main.Albums.AddListeningAsync(Album.Id, NewListeningDate.Value);
            await RefreshHistoryAsync(); Main.Message = "Прослушивание добавлено в историю.";
        }, Main.ReportError);
        RemoveListeningCommand = Main.CreateCommand(async p =>
        {
            if (p is not ListeningSession session || !Main.Dialogs.Confirm("Удалить эту запись прослушивания?")) return;
            await Main.Albums.RemoveListeningAsync(session.Id); await RefreshHistoryAsync();
        }, Main.ReportError);
        AddTrackCommand = new(_ => Tracks.Add(new TrackItemViewModel()));
        RemoveTrackCommand = new(p => { if (p is TrackItemViewModel track) Tracks.Remove(track); });
        SaveTracksCommand = Main.CreateCommand(async _ =>
        {
            await Main.Albums.SaveTracksAsync(Album.Id, Tracks.Select(t => t.ToModel()).ToList());
            await Main.ReloadAsync(); Main.OpenAlbum(Album.Id, true); Main.Message = "Треки и заметки сохранены.";
        }, Main.ReportError);
    }
    private string Snapshot() => JsonSerializer.Serialize(Tracks.Select(t => t.ToModel()));
    private async Task RefreshHistoryAsync()
    {
        await Main.ReloadAsync();
        var current = Main.AllAlbums.Single(a => a.Id == Album.Id);
        Album.ListeningSessions = current.ListeningSessions; Album.ListeningDate = current.ListeningDate; Album.Status = current.Status;
        Raise(nameof(Album)); Raise(nameof(History)); Raise(nameof(NoHistory));
    }
}
