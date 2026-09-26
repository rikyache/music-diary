using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using MusicDiary.Data;
using MusicDiary.Services;
using MusicDiary.ViewModels;
using MusicDiary.Views;

namespace MusicDiary;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
        var main = new MainViewModel(new AlbumService(() => new DiaryDbContext()), new DialogService());
        var window = new MainWindow { DataContext = main };
        MainWindow = window; window.Show();
        try { await main.InitializeAsync(); }
        catch (Exception ex) { main.ReportError(ex); }
    }
}
