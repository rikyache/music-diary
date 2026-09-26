using MusicDiary.Models;

namespace MusicDiary.Services;

public record ChartBar(string Label, int Count, double Width);

public class StatisticsService
{
    public int ListenedCount(IEnumerable<Album> albums) => albums.Count(a => a.Status == AlbumStatus.Listened);
    public string AverageRating(IEnumerable<Album> albums)
    {
        var ratings = albums.Where(a => a.Rating.HasValue).Select(a => a.Rating!.Value).ToArray();
        return ratings.Length == 0 ? "—" : ratings.Average().ToString("0.0");
    }
    public List<ChartBar> Ratings(IEnumerable<Album> albums) => Bars(Enumerable.Range(1, 10)
        .Select(r => (r.ToString(), albums.Count(a => a.Rating == r))));
    public List<ChartBar> Genres(IEnumerable<Album> albums) => Bars(albums.GroupBy(a => a.Genre, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => (g.Key, g.Count())));
    public List<ChartBar> Artists(IEnumerable<Album> albums) => Bars(albums.GroupBy(a => a.Artist, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Take(10).Select(g => (g.Key, g.Count())));
    public List<ChartBar> Months(IEnumerable<Album> albums)
    {
        var sessions = albums.SelectMany(a => a.ListeningSessions).ToArray();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-11);
        return Bars(Enumerable.Range(0, 12).Select(i => start.AddMonths(i)).Select(month =>
            (month.ToString("MMM yy"), sessions.Where(s => s.ListenedAt.Year == month.Year && s.ListenedAt.Month == month.Month)
                .Select(s => s.AlbumId).Distinct().Count())));
    }
    private static List<ChartBar> Bars(IEnumerable<(string Label, int Count)> source)
    {
        var values = source.ToList();
        var max = Math.Max(1, values.Count == 0 ? 0 : values.Max(v => v.Count));
        return values.Select(v => new ChartBar(v.Label, v.Count, 270.0 * v.Count / max)).ToList();
    }
}
