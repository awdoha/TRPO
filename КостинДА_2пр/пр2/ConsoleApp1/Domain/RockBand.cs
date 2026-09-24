namespace MusicCatalog.Domain; // пространство имён модели

// Рок-группа: наследует MusicBand. sealed = «запечатан»: от RockBand наследоваться нельзя (финальный тип; позволяет также компилятору оптимизировать вызовы).
// Используется: создаётся в ConsoleApp.SeedIfEmpty (Queen, Nirvana), ConsoleApp.AddBandInteractive (тип 1), RepositoryFileStore.FromDto (пункт 9)
public sealed class RockBand : MusicBand
{
    // Уникальная характеристика рок-группы: наличие живых концертных альбомов
    // Только get — задаётся в конструкторе. Используется: RockBand.ToString и RepositoryFileStore.ToDto (сохранение в JSON, поле HasLiveAlbums)
    public bool HasLiveAlbums { get; }

    // override = реализуем абстрактное свойство базового MusicBand.AlbumCount. Значение задаётся в конструкторе.
    // Используется: MusicBand.CompareTo, ToString, MusicBandRepository.Search/GetStats, RepositoryFileStore.ToDto — везде через тип MusicBand (полиморфизм)
    public override int AlbumCount { get; }

    // Конструктор рок-группы. Вызывается из ConsoleApp.SeedIfEmpty, AddBandInteractive и RepositoryFileStore.FromDto
    public RockBand(
        // название (в базовый конструктор)
        string name,
        // жанр (в базовый конструктор)
        string genre,
        // год основания (в базовый конструктор)
        int formedYear,
        // лидер-артист (в базовый конструктор)
        Artist leadArtist,
        // есть ли концертные альбомы — своё поле RockBand
        bool hasLiveAlbums,
        // количество альбомов — хранится в override-свойстве AlbumCount
        int albumCount,
        // необязательный Id: null — автоматически, число — при загрузке из JSON
        int? id = null) : base(name, genre, formedYear, leadArtist, id) // сначала выполняется конструктор MusicBand (проверки и Id), затем тело ниже
    {
        // Число альбомов не может быть отрицательным; исключение ловит try/catch в ConsoleApp.Run
        if (albumCount < 0) throw new ArgumentOutOfRangeException(nameof(albumCount));
        // Сохраняем признак концертных альбомов
        HasLiveAlbums = hasLiveAlbums;
        // Сохраняем количество альбомов
        AlbumCount = albumCount;
    } // конец конструктора RockBand

    // override ToString: base.ToString() + «Концертные альбомы: Да/Нет». Используется: вывод пунктов меню 1, 5 и «Добавлено»
    public override string ToString()
        => $"{base.ToString()}, Концертные альбомы: {(HasLiveAlbums ? "Да" : "Нет")}";
} // конец класса RockBand
