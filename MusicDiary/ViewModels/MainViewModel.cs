using MusicDiary.Commands;
using MusicDiary.Helpers;
using MusicDiary.Models;
using MusicDiary.Services;

namespace MusicDiary.ViewModels;

public class MainViewModel : ObservableObject
{
    public AlbumService Albums { get; }
    public DialogService Dialogs { get; }
    public CoverService Covers { get; } = new();
    public StatisticsService Statistics { get; } = new();
    public List<Album> AllAlbums { get; private set; } = [];
    private PageViewModel? currentPage;
    public PageViewModel? CurrentPage { get => currentPage; private set => Set(ref currentPage, value); }
    private string message = "";
    public string Message { get => message; set => Set(ref message, value); }
    private bool isBusy;
    public bool IsBusy { get => isBusy; private set => Set(ref isBusy, value); }
    private string section = "Главная";
    public string Section { get => section; private set => Set(ref section, value); }
    public RelayCommand NavigateCommand { get; }
    public RelayCommand AddAlbumCommand { get; }
    public RelayCommand OpenAlbumCommand { get; }
    public RelayCommand DismissMessageCommand { get; }
    public AsyncRelayCommand RetryCommand { get; }

    public MainViewModel(AlbumService albums, DialogService dialogs)
    {
        Albums = albums; Dialogs = dialogs;
        NavigateCommand = new(p => Navigate(p?.ToString() ?? "Главная"));
        AddAlbumCommand = new(_ => EditAlbum(null));
        OpenAlbumCommand = new(p => { if (p is Album album) OpenAlbum(album.Id); });
        DismissMessageCommand = new(_ => Message = "");
        RetryCommand = new(_ => InitializeAsync(), ReportError);
    }
    public async Task InitializeAsync()
    {
        IsBusy = true;
        try { await Albums.InitializeAsync(); await ReloadAsync(); Navigate("Главная", true); }
        finally { IsBusy = false; }
    }
    public async Task ReloadAsync() => AllAlbums = await Albums.GetAllAsync();
    public bool CanLeave() => !IsBusy && (CurrentPage?.HasUnsavedChanges != true || Dialogs.Confirm("Есть несохранённые изменения. Покинуть страницу и отменить их?"));
    public AsyncRelayCommand CreateCommand(Func<object?, Task> execute, Action<Exception>? onError = null) => new(async parameter =>
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await execute(parameter); }
        finally { IsBusy = false; }
    }, onError ?? ReportError);
    public void Navigate(string page, bool force = false)
    {
        if (!force && !CanLeave()) return;
        Message = ""; Section = page;
        CurrentPage = page switch
        {
            "Каталог" => new CatalogViewModel(this),
            "Избранное" => new FavoritesViewModel(this),
            "Статистика" => new StatisticsViewModel(this),
            _ => new DashboardViewModel(this)
        };
    }
    public void OpenAlbum(int id, bool force = false)
    {
        if (!force && !CanLeave()) return;
        var album = AllAlbums.FirstOrDefault(a => a.Id == id);
        if (album is null) { Navigate("Каталог", true); return; }
        Message = ""; Section = "Альбом";
        CurrentPage = new AlbumDetailsViewModel(this, album);
    }
    public void EditAlbum(Album? album)
    {
        if (!CanLeave()) return;
        Message = ""; Section = album is null ? "Новая запись" : "Редактирование";
        CurrentPage = new AlbumEditorViewModel(this, album);
    }
    public void ReportError(Exception ex)
    {
        Message = ex is ArgumentException ? ex.Message : "Не удалось выполнить действие. Проверьте доступ к файлам и повторите попытку. " + ex.GetBaseException().Message;
    }
}
