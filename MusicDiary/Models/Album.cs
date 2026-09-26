namespace MusicDiary.Models;

public class Album
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = "";
    public string? CoverPath { get; set; }
    public int? Rating { get; set; }
    public string Review { get; set; } = "";
    public DateTime? ListeningDate { get; set; }
    public AlbumStatus Status { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Track> Tracks { get; set; } = [];
    public List<ListeningSession> ListeningSessions { get; set; } = [];
}
