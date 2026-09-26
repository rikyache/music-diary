using System.IO;
using System.Windows.Media.Imaging;
using MusicDiary.Data;

namespace MusicDiary.Services;

public class CoverService(string? dataDirectory = null)
{
    // Copy to the application's directory so moving the original does not break the cover.
    public async Task<string?> ImportAsync(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        var directory = Path.Combine(dataDirectory ?? DiaryDbContext.DataDirectory, "Covers");
        Directory.CreateDirectory(directory);
        if (Path.GetDirectoryName(Path.GetFullPath(source)) == directory) return source;
        if (!File.Exists(source)) throw new ArgumentException("Файл обложки не найден. Выберите его заново.");
        if (new FileInfo(source).Length > 20 * 1024 * 1024) throw new ArgumentException("Размер обложки не должен превышать 20 МБ.");
        using (var stream = File.OpenRead(source))
        {
            try { _ = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad); }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException)
            { throw new ArgumentException("Не удалось прочитать изображение. Выберите PNG или JPEG."); }
        }
        var target = Path.Combine(directory, Guid.NewGuid().ToString("N") + Path.GetExtension(source));
        await using var input = File.OpenRead(source);
        await using var output = File.Create(target);
        await input.CopyToAsync(output);
        return target;
    }
}
