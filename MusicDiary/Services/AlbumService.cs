using Microsoft.EntityFrameworkCore;
using MusicDiary.Data;
using MusicDiary.Models;

namespace MusicDiary.Services;

public class AlbumService(Func<DiaryDbContext> createContext)
{
    public async Task InitializeAsync()
    {
        await using var db = createContext();
        await db.Database.MigrateAsync();
    }

    public async Task<List<Album>> GetAllAsync()
    {
        await using var db = createContext();
        return await db.Albums.AsNoTracking().Include(a => a.Tracks)
            .Include(a => a.ListeningSessions).AsSplitQuery().ToListAsync();
    }

    public static void Validate(Album album)
    {
        if (string.IsNullOrWhiteSpace(album.Title) || album.Title.Trim().Length > 200)
            throw new ArgumentException("Укажите название альбома (до 200 символов).");
        if (string.IsNullOrWhiteSpace(album.Artist) || album.Artist.Trim().Length > 200)
            throw new ArgumentException("Укажите исполнителя (до 200 символов).");
        if (album.ReleaseYear < 1000 || album.ReleaseYear > DateTime.Today.Year + 2)
            throw new ArgumentException($"Год выпуска должен быть от 1000 до {DateTime.Today.Year + 2}.");
        if (string.IsNullOrWhiteSpace(album.Genre) || album.Genre.Trim().Length > 80)
            throw new ArgumentException("Укажите жанр (до 80 символов).");
        if (album.Rating is < 1 or > 10) throw new ArgumentException("Оценка должна быть от 1 до 10.");
        if (!Enum.IsDefined(album.Status)) throw new ArgumentException("Выберите статус альбома.");
        if (album.ListeningDate > DateTime.Today) throw new ArgumentException("Дата прослушивания не может быть в будущем.");
        if (album.Status == AlbumStatus.Listened && album.ListeningDate is null)
            throw new ArgumentException("Для прослушанного альбома укажите дату прослушивания.");
        if (album.Tracks.Any(t => string.IsNullOrWhiteSpace(t.Title) || t.Title.Trim().Length > 200))
            throw new ArgumentException("У каждого трека должно быть название (до 200 символов).");
    }

    public async Task<int> SaveAsync(Album input)
    {
        Validate(input);
        await using var db = createContext();
        Album album;
        if (input.Id == 0)
        {
            album = new Album();
            db.Albums.Add(album);
        }
        else
        {
            album = await db.Albums.Include(a => a.Tracks).Include(a => a.ListeningSessions)
                .SingleAsync(a => a.Id == input.Id);
        }

        album.Title = input.Title.Trim();
        album.Artist = input.Artist.Trim();
        album.ReleaseYear = input.ReleaseYear;
        album.Genre = input.Genre.Trim();
        album.CoverPath = input.CoverPath;
        album.Rating = input.Rating;
        album.Review = input.Review.Trim();
        album.Status = input.Status;
        album.IsFavorite = input.IsFavorite;

        // Editing the latest date corrects that session; adding a repeat is a separate action.
        var latest = album.ListeningSessions.OrderByDescending(s => s.ListenedAt).ThenByDescending(s => s.Id).FirstOrDefault();
        if (input.ListeningDate?.Date != album.ListeningDate?.Date)
        {
            if (input.ListeningDate is { } date)
            {
                if (latest is null) album.ListeningSessions.Add(new ListeningSession { ListenedAt = date.Date });
                else latest.ListenedAt = date.Date;
            }
            else if (latest is not null)
            {
                album.ListeningSessions.Remove(latest);
                db.ListeningSessions.Remove(latest);
            }
        }
        album.ListeningDate = album.ListeningSessions.Count == 0 ? null : album.ListeningSessions.Max(s => s.ListenedAt);

        foreach (var removed in album.Tracks.Where(t => input.Tracks.All(i => i.Id != t.Id)).ToList())
        {
            album.Tracks.Remove(removed);
            db.Tracks.Remove(removed);
        }
        foreach (var item in input.Tracks)
        {
            var track = item.Id == 0 ? null : album.Tracks.SingleOrDefault(t => t.Id == item.Id);
            if (track is null) { track = new Track(); album.Tracks.Add(track); }
            track.Title = item.Title.Trim();
            track.IsFavorite = item.IsFavorite;
            track.Note = item.Note.Trim();
        }
        await db.SaveChangesAsync();
        return album.Id;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = createContext();
        var album = await db.Albums.FindAsync(id);
        if (album is null) return;
        db.Albums.Remove(album);
        await db.SaveChangesAsync();
    }

    public async Task ToggleFavoriteAsync(int id)
    {
        await using var db = createContext();
        var album = await db.Albums.SingleAsync(a => a.Id == id);
        album.IsFavorite = !album.IsFavorite;
        await db.SaveChangesAsync();
    }

    public async Task AddListeningAsync(int albumId, DateTime date)
    {
        if (date.Date > DateTime.Today) throw new ArgumentException("Дата прослушивания не может быть в будущем.");
        await using var db = createContext();
        var album = await db.Albums.SingleAsync(a => a.Id == albumId);
        db.ListeningSessions.Add(new ListeningSession { AlbumId = albumId, ListenedAt = date.Date });
        if (album.ListeningDate is null || date.Date > album.ListeningDate) album.ListeningDate = date.Date;
        album.Status = AlbumStatus.Listened;
        await db.SaveChangesAsync();
    }

    public async Task RemoveListeningAsync(int id)
    {
        await using var db = createContext();
        var session = await db.ListeningSessions.SingleAsync(s => s.Id == id);
        var album = await db.Albums.Include(a => a.ListeningSessions).SingleAsync(a => a.Id == session.AlbumId);
        album.ListeningSessions.Remove(session);
        db.ListeningSessions.Remove(session);
        album.ListeningDate = album.ListeningSessions.Count == 0 ? null : album.ListeningSessions.Max(s => s.ListenedAt);
        if (album.ListeningDate is null && album.Status == AlbumStatus.Listened) album.Status = AlbumStatus.WantToListen;
        await db.SaveChangesAsync();
    }

    public async Task SaveTracksAsync(int albumId, List<Track> tracks)
    {
        if (tracks.Any(t => string.IsNullOrWhiteSpace(t.Title) || t.Title.Trim().Length > 200))
            throw new ArgumentException("У каждого трека должно быть название (до 200 символов).");
        await using var db = createContext();
        var album = await db.Albums.Include(a => a.Tracks).SingleAsync(a => a.Id == albumId);
        foreach (var old in album.Tracks.Where(t => tracks.All(i => i.Id != t.Id)).ToList()) db.Tracks.Remove(old);
        foreach (var item in tracks)
        {
            var track = item.Id == 0 ? null : album.Tracks.Single(t => t.Id == item.Id);
            if (track is null) { track = new Track(); album.Tracks.Add(track); }
            track.Title = item.Title.Trim(); track.Note = item.Note.Trim(); track.IsFavorite = item.IsFavorite;
        }
        await db.SaveChangesAsync();
    }
}
