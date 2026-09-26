using MusicDiary.Helpers;

namespace MusicDiary.ViewModels;

public abstract class PageViewModel(MainViewModel main) : ObservableObject
{
    protected MainViewModel Main { get; } = main;
    public virtual bool HasUnsavedChanges => false;
}
