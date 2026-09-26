using MusicDiary.Services;

namespace MusicDiary.ViewModels;

public class StatisticsViewModel(MainViewModel main) : PageViewModel(main)
{
    public int ListenedCount => Main.Statistics.ListenedCount(Main.AllAlbums);
    public string AverageRating => Main.Statistics.AverageRating(Main.AllAlbums);
    public int SessionCount => Main.AllAlbums.Sum(a => a.ListeningSessions.Count);
    public int TotalCount => Main.AllAlbums.Count;
    public bool IsEmpty => TotalCount == 0;
    public List<ChartBar> Ratings { get; } = main.Statistics.Ratings(main.AllAlbums);
    public List<ChartBar> Genres { get; } = main.Statistics.Genres(main.AllAlbums);
    public List<ChartBar> Artists { get; } = main.Statistics.Artists(main.AllAlbums);
    public List<ChartBar> Months { get; } = main.Statistics.Months(main.AllAlbums);
}
