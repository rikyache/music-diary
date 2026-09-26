using System.Windows;
using Microsoft.Win32;

namespace MusicDiary.Services;

public class DialogService
{
    public virtual bool Confirm(string text) => MessageBox.Show(text, "Личный музыкальный дневник",
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    public virtual string? PickCover()
    {
        var dialog = new OpenFileDialog { Title = "Обложка альбома", Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
