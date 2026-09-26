using MusicDiary.Helpers;
using MusicDiary.Models;

namespace MusicDiary.ViewModels;

public class TrackItemViewModel : ObservableObject
{
    public int Id { get; }
    private string title, note;
    private bool isFavorite;
    public string Title { get => title; set => Set(ref title, value); }
    public string Note { get => note; set => Set(ref note, value); }
    public bool IsFavorite { get => isFavorite; set => Set(ref isFavorite, value); }
    public TrackItemViewModel(Track? track = null)
    {
        Id = track?.Id ?? 0; title = track?.Title ?? ""; note = track?.Note ?? ""; isFavorite = track?.IsFavorite ?? false;
    }
    public Track ToModel() => new() { Id = Id, Title = Title, Note = Note, IsFavorite = IsFavorite };
}
