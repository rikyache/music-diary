using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MusicDiary.Models;
using MusicDiary.Services;
using MusicDiary.Helpers;
using MusicDiary.ViewModels;
using MusicDiary.Views;

namespace MusicDiary.Tests;

public class UiSmokeTests
{
    [Fact]
    public async Task AllScreensRenderWithoutBindingErrorsAtTwoWindowSizes()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
            {
                try { await RenderScreensAsync(); completion.SetResult(); }
                catch (Exception ex) { completion.SetException(ex); }
                finally { app.Shutdown(); }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA); thread.IsBackground = true; thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(60));
    }

    private static async Task RenderScreensAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        using var db = new TestDatabase();
        var main = new MainViewModel(db.Service, new TestDialogs()); await main.InitializeAsync();
        using var errors = new StringWriter();
        var listener = new TextWriterTraceListener(errors);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        var window = new MainWindow { DataContext = main };
        var root = (FrameworkElement)window.Content;
        var pageScroll = (ScrollViewer)window.FindName("PageScroll");
        var output = Path.Combine(AppContext.BaseDirectory, "ui-previews"); Directory.CreateDirectory(output);
        try
        {
            await RenderAsync("01-empty");
            // Exercise cover import on temporary files, never in the user's diary directory.
            var sourceCover = Path.Combine(output, "test-cover.png");
            var coverEncoder = new PngBitmapEncoder();
            coverEncoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
                new byte[] { 80, 130, 210, 255, 80, 130, 210, 255, 60, 80, 160, 255, 60, 80, 160, 255 }, 8)));
            using (var file = File.Create(sourceCover)) coverEncoder.Save(file);
            var covers = new CoverService(output);
            var imported = await covers.ImportAsync(sourceCover);
            File.Delete(sourceCover);
            Assert.True(File.Exists(imported));
            Assert.IsType<BitmapImage>(new CoverConverter().Convert(imported!, typeof(ImageSource), null!, CultureInfo.CurrentCulture));
            Assert.Equal(imported, await covers.ImportAsync(imported));
            var invalid = Path.Combine(output, "invalid.png"); await File.WriteAllTextAsync(invalid, "not an image");
            await Assert.ThrowsAsync<ArgumentException>(() => covers.ImportAsync(invalid));
            File.Delete(invalid); File.Delete(imported!);
            var first = TestDatabase.Album(); first.IsFavorite = true;
            await db.Service.SaveAsync(first);
            var second = TestDatabase.Album("Когда город уснёт", "Северный ветер", 9); second.Genre = "Dream pop";
            await db.Service.SaveAsync(second);
            var third = TestDatabase.Album("Сторона Б", "Комната", null); third.Status = AlbumStatus.WantToListen; third.ListeningDate = null;
            await db.Service.SaveAsync(third); await main.ReloadAsync();
            foreach (var size in new[] { new Size(1380, 900), new Size(1120, 720) })
            {
                window.Width = size.Width; window.Height = size.Height;
                main.Navigate("Главная", true); await RenderAsync($"02-dashboard-{size.Width}");
                main.Navigate("Каталог", true); await RenderAsync($"03-catalog-{size.Width}");
                main.Navigate("Избранное", true); await RenderAsync($"04-favorites-{size.Width}");
                main.Navigate("Статистика", true); await RenderAsync($"05-statistics-{size.Width}");
                main.OpenAlbum(main.AllAlbums[0].Id, true); await RenderAsync($"06-details-{size.Width}");
                pageScroll.ScrollToEnd(); await RenderAsync($"06-details-bottom-{size.Width}");
                main.EditAlbum(main.AllAlbums[0]); await RenderAsync($"07-editor-{size.Width}");
                pageScroll.ScrollToEnd(); await RenderAsync($"07-editor-bottom-{size.Width}");
            }
            // Verify real WPF bindings propagate user edits to the editor.
            var editor = Assert.IsType<AlbumEditorViewModel>(main.CurrentPage);
            var statusBox = Descendants<ComboBox>(root).First(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Статус");
            statusBox.SelectedIndex = 1;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(AlbumStatus.Listening, editor.Status);
            var titleBox = Descendants<TextBox>(root).First(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Название альбома");
            titleBox.Text = "Изменено через WPF";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal("Изменено через WPF", editor.Title);
            await editor.SaveCommand.ExecuteAsync();
            Assert.Contains(await db.Service.GetAllAsync(), a => a.Title == "Изменено через WPF");
            listener.Flush(); Assert.DoesNotContain("Error:", errors.ToString());
        }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(listener); window.Close(); }

        async Task RenderAsync(string name)
        {
            root.Measure(new Size(window.Width, window.Height)); root.Arrange(new Rect(0, 0, window.Width, window.Height)); root.UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); root.UpdateLayout();
            Assert.NotEmpty(Descendants<UserControl>(root));
            var image = new RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, PixelFormats.Pbgra32);
            image.Render(root); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
