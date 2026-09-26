using System.ComponentModel;
using System.Windows;
using MusicDiary.ViewModels;

namespace MusicDiary.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is MainViewModel main && !main.CanLeave()) e.Cancel = true;
        base.OnClosing(e);
    }
}
