using MusicDiary.Models;

namespace MusicDiary.ViewModels;

public class DashboardViewModel(MainViewModel main) : PageViewModel(main)
{
    public List<Album> RecentAlbums { get; } = main.AllAlbums.OrderByDescending(a => a.CreatedAt).Take(6).ToList();
    public List<Album> RecentlyListened { get; } = main.AllAlbums.Where(a => a.ListeningDate != null)
        .OrderByDescending(a => a.ListeningDate).Take(6).ToList();
    public int ListenedCount => Main.Statistics.ListenedCount(Main.AllAlbums);
    public string AverageRating => Main.Statistics.AverageRating(Main.AllAlbums);
    public int FavoriteCount => Main.AllAlbums.Count(a => a.IsFavorite);
    public bool IsEmpty => RecentAlbums.Count == 0;
    public bool NoListening => RecentlyListened.Count == 0;
}
