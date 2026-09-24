namespace MusicCatalog.Domain; // пространство имён модели

// Поп-группа: наследует MusicBand (двоеточие = «является MusicBand»). НЕ sealed — от неё теоретически можно наследоваться.
// Используется: создаётся в ConsoleApp.SeedIfEmpty (BLACKPINK), ConsoleApp.AddBandInteractive (тип 2), RepositoryFileStore.FromDto (пункт 9)
public class PopBand : MusicBand
{
    // Уникальная характеристика поп-группы: женский состав
    // Только get — задаётся в конструкторе. Используется: PopBand.ToString и RepositoryFileStore.ToDto (сохранение в JSON, поле IsGirlGroup)
    public bool IsGirlGroup { get; }

    // override = реализуем абстрактное свойство базового MusicBand.AlbumCount (без override программа не скомпилируется). Значение задаётся в конструкторе.
    // Используется: MusicBand.CompareTo, ToString, MusicBandRepository.Search/GetStats, RepositoryFileStore.ToDto — везде через тип MusicBand (полиморфизм)
    public override int AlbumCount { get; }

    // Конструктор поп-группы. Вызывается из ConsoleApp.SeedIfEmpty, AddBandInteractive и RepositoryFileStore.FromDto
    public PopBand(
        // название (передаётся дальше в базовый конструктор)
        string name,
        // жанр (в базовый конструктор)
        string genre,
        // год основания (в базовый конструктор)
        int formedYear,
        // лидер-артист (в базовый конструктор)
        Artist leadArtist,
        // признак женской группы — своё поле PopBand
        bool isGirlGroup,
        // количество альбомов — хранится в override-свойстве AlbumCount
        int albumCount,
        // необязательный Id: null — выдать автоматически, число — при загрузке из JSON
        int? id = null) : base(name, genre, formedYear, leadArtist, id) // сначала вызывается конструктор MusicBand (проверки общих полей и Id), затем тело ниже
    {
        // Число альбомов не может быть отрицательным; исключение ловит try/catch в ConsoleApp.Run
        if (albumCount < 0) throw new ArgumentOutOfRangeException(nameof(albumCount));
        // Сохраняем признак женской группы
        IsGirlGroup = isGirlGroup;
        // Сохраняем количество альбомов (для get-only авто-свойства присваивание допустимо в конструкторе)
        AlbumCount = albumCount;
    } // конец конструктора PopBand

    // override ToString: берём строку базового класса (base.ToString()) и дописываем «Женская группа: Да/Нет» (тернарный оператор ?: выбирает текст). Используется: вывод пунктов меню 1, 5 и «Добавлено»
    public override string ToString()
        => $"{base.ToString()}, Женская группа: {(IsGirlGroup ? "Да" : "Нет")}";
} // конец класса PopBand
