using System.Collections.ObjectModel;
using MusicDiary.Commands;
using MusicDiary.Helpers;
using MusicDiary.Models;

namespace MusicDiary.ViewModels;

public class CatalogViewModel : PageViewModel
{
    private readonly bool favoritesOnly;
    public string Title => favoritesOnly ? "Ближе к сердцу" : "Ваша коллекция";
    public string Subtitle => favoritesOnly ? "Альбомы, к которым хочется возвращаться." : "Музыка, которую вы открыли. Впечатления, которые остались.";
    public ObservableCollection<Album> Albums { get; } = [];
    public List<string> Genres { get; }
    public List<StatusOption> Statuses => DisplayOptions.FilterStatuses;
    public List<string> Ratings { get; } = ["Любая оценка", "Без оценки", .. Enumerable.Range(1, 10).Select(i => i.ToString())];
    public List<string> SortOptions { get; } = ["Сначала новые записи", "Сначала старые записи", "Год: новые сначала", "Год: старые сначала", "Оценка: по убыванию", "Оценка: по возрастанию"];
    private string titleSearch = "", artistSearch = "", genre = "Все жанры", rating = "Любая оценка", sort = "Сначала новые записи";
    private StatusOption status = DisplayOptions.FilterStatuses[0];
    public string TitleSearch { get => titleSearch; set { if (Set(ref titleSearch, value)) Filter(); } }
    public string ArtistSearch { get => artistSearch; set { if (Set(ref artistSearch, value)) Filter(); } }
    public string SelectedGenre { get => genre; set { if (Set(ref genre, value)) Filter(); } }
    public string SelectedRating { get => rating; set { if (Set(ref rating, value)) Filter(); } }
    public string SelectedSort { get => sort; set { if (Set(ref sort, value)) Filter(); } }
    public StatusOption SelectedStatus { get => status; set { if (Set(ref status, value)) Filter(); } }
    public bool IsEmpty => Albums.Count == 0;
    public string ResultText => $"Найдено альбомов: {Albums.Count}";
    public RelayCommand ResetCommand { get; }
    public CatalogViewModel(MainViewModel main, bool favoritesOnly = false) : base(main)
    {
        this.favoritesOnly = favoritesOnly;
        Genres = ["Все жанры", .. main.AllAlbums.Select(a => a.Genre).Distinct(StringComparer.OrdinalIgnoreCase).Order()];
        ResetCommand = new(_ => { TitleSearch = ""; ArtistSearch = ""; SelectedGenre = Genres[0]; SelectedRating = Ratings[0]; SelectedStatus = Statuses[0]; SelectedSort = SortOptions[0]; });
        Filter();
    }
    private void Filter()
    {
        IEnumerable<Album> query = Main.AllAlbums.Where(a => !favoritesOnly || a.IsFavorite);
        query = query.Where(a => a.Title.Contains(TitleSearch.Trim(), StringComparison.OrdinalIgnoreCase)
            && a.Artist.Contains(ArtistSearch.Trim(), StringComparison.OrdinalIgnoreCase));
        if (SelectedGenre != "Все жанры") query = query.Where(a => a.Genre.Equals(SelectedGenre, StringComparison.OrdinalIgnoreCase));
        if (SelectedStatus?.Value is { } selected) query = query.Where(a => a.Status == selected);
        if (SelectedRating == "Без оценки") query = query.Where(a => a.Rating == null);
        else if (int.TryParse(SelectedRating, out var score)) query = query.Where(a => a.Rating == score);
        query = SelectedSort switch
        {
            "Сначала старые записи" => query.OrderBy(a => a.CreatedAt),
            "Год: новые сначала" => query.OrderByDescending(a => a.ReleaseYear),
            "Год: старые сначала" => query.OrderBy(a => a.ReleaseYear),
            "Оценка: по убыванию" => query.OrderBy(a => a.Rating == null).ThenByDescending(a => a.Rating),
            "Оценка: по возрастанию" => query.OrderBy(a => a.Rating == null).ThenBy(a => a.Rating),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };
        Albums.Clear();
        foreach (var album in query) Albums.Add(album);
        Raise(nameof(IsEmpty)); Raise(nameof(ResultText));
    }
}
