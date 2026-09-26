namespace MusicDiary.Models;

public class ListeningSession
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public DateTime ListenedAt { get; set; }
}
