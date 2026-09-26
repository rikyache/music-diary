using MusicDiary.Models;

namespace MusicDiary.Helpers;

public record StatusOption(AlbumStatus? Value, string Label)
{
    public override string ToString() => Label;
}
public static class DisplayOptions
{
    public static List<StatusOption> Statuses { get; } =
    [new(AlbumStatus.WantToListen, "Хочу послушать"), new(AlbumStatus.Listening, "Слушаю"), new(AlbumStatus.Listened, "Прослушан")];
    public static List<StatusOption> FilterStatuses { get; } = [new(null, "Все статусы"), .. Statuses];
    public static string StatusName(AlbumStatus status) => Statuses.First(s => s.Value == status).Label;
}
