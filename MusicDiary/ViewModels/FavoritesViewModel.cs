namespace MusicDiary.ViewModels;

public class FavoritesViewModel(MainViewModel main) : CatalogViewModel(main, favoritesOnly: true);
