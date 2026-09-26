namespace MusicDiary.Models;

public class Track
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int AlbumId { get; set; }
    public bool IsFavorite { get; set; }
    public string Note { get; set; } = "";
}
