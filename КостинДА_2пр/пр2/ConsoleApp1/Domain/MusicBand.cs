namespace MusicCatalog.Domain; // пространство имён модели; подключается в Repository, Storage, UI

// Абстрактный базовый класс «музыкальная группа». abstract = нельзя написать new MusicBand(...), только через наследников RockBand/PopBand.
// IComparable<MusicBand> = группы можно упорядочивать (_bands.Sort()); IEquatable<MusicBand> = группы можно сравнивать на равенство (по Id).
// Используется: как тип элементов списка в MusicBandRepository, как база для RockBand и PopBand, в DTO-конвертации RepositoryFileStore
public abstract class MusicBand : IComparable<MusicBand>, IEquatable<MusicBand>
{
    // Счётчик для автоприсвоения Id. static = один на ВСЕ группы (общий), private = снаружи не виден. Используется: конструктор ниже (Id = id ?? _nextId++) и свойство NextId
    private static int _nextId = 1;

    // Следующий Id, который будет выдан (только чтение). Используется: в коде приложения не вызывается (справочное свойство)
    public static int NextId => _nextId;

    // Уникальный номер группы; только get — задаётся в конструкторе. Используется: Equals/GetHashCode, MusicBandRepository.Add/RemoveById, ConsoleApp.RemoveInteractive (пункт 3), ToString, RepositoryFileStore.ToDto
    public int Id { get; }
    // Название группы. Используется: DisplayName, MusicBandRepository (индексатор по имени, FindByName, Search), RepositoryFileStore.ToDto
    public string Name { get; }
    // Жанр. Используется: DisplayName, MusicBandRepository.Search (пункт 5), RepositoryFileStore.ToDto
    public string Genre { get; }
    // Год основания. Используется: RepositoryFileStore.ToDto (сохранение в JSON) и Load -> FromDto
    public int FormedYear { get; }
    // Лидер-артист (объект Artist). Используется: ToString, MusicBandRepository.GetStats (средний стаж), RepositoryFileStore.Save/ToDto (ArtistId = LeadArtist.Key)
    public Artist LeadArtist { get; }

    // Количество альбомов — различается у конкретных типов групп
    // abstract = здесь только «контракт» (без реализации): наследники ОБЯЗАНЫ дать реализацию через override (см. RockBand, PopBand).
    // Используется: CompareTo (сортировка), ToString, MusicBandRepository.Search (minAlbums) и GetStats (среднее), RepositoryFileStore.ToDto
    public abstract int AlbumCount { get; }

    // Конструктор protected: вызвать его можно только из наследников через `: base(...)` (RockBand/PopBand). `int? id = null` — необязательный Id: null -> выдать автоматически, число -> взять из JSON при загрузке
    protected MusicBand(string name, string genre, int formedYear, Artist leadArtist, int? id = null)
    {
        // Обрезаем пробелы; `?? string.Empty` защищает от null
        Name = (name ?? string.Empty).Trim();
        // То же для жанра
        Genre = (genre ?? string.Empty).Trim();

        // Название обязательно; исключение ловит try/catch в ConsoleApp.Run
        if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("Name is required.");
        // Жанр обязателен
        if (string.IsNullOrWhiteSpace(Genre)) throw new ArgumentException("Genre is required.");
        // Год основания должен быть в диапазоне 1900..текущий год
        if (formedYear < 1900 || formedYear > DateTime.Now.Year)
            // nameof(formedYear) — имя параметра для сообщения об ошибке
            throw new ArgumentOutOfRangeException(nameof(formedYear), "FormedYear must be between 1900 and current year.");

        // Если лидер = null, оператор `??` выбрасывает ArgumentNullException (throw-выражение); иначе сохраняем лидера
        LeadArtist = leadArtist ?? throw new ArgumentNullException(nameof(leadArtist));
        // Сохраняем год основания (уже проверенный)
        FormedYear = formedYear;
        // Если id передан (загрузка из JSON) — берём его, иначе берём _nextId и увеличиваем счётчик (постфиксный ++)
        Id = id ?? _nextId++;
    } // конец конструктора MusicBand

    // Отображаемое имя "Название (Жанр)"; virtual = наследник МОГ БЫ переопределить (в проекте не переопределяется). Используется: CompareTo, BandByNameComparer.Compare, ToString
    public virtual string DisplayName => $"{Name} ({Genre})";

    // Реализация IComparable<MusicBand>: порядок «по умолчанию». Используется: MusicBandRepository.SortByDefault (_bands.Sort()) и операторы >, <, >=, <= ниже
    public int CompareTo(MusicBand? other)
    {
        // Любой объект «больше» null
        if (other is null) return 1;
        // Сортировка: сначала по количеству альбомов, затем по DisplayName
        // AlbumCount у каждого типа свой (полиморфизм: вызовется override из RockBand/PopBand)
        int byAlbums = AlbumCount.CompareTo(other.AlbumCount);
        // Если альбомов разное число — результат готов
        if (byAlbums != 0) return byAlbums;
        // При равенстве — по названию без учёта регистра
        return string.Compare(DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase);
    } // конец метода CompareTo

    // Реализация IEquatable<MusicBand>: группы равны, если совпадает Id (не имя!). Используется: Equals(object), оператор ==, List.Remove в MusicBandRepository.Remove
    public bool Equals(MusicBand? other) => other is not null && Id == other.Id;
    // Переопределение object.Equals: `obj is MusicBand b` — проверка типа + приведение; дальше типизированный Equals
    public override bool Equals(object? obj) => obj is MusicBand b && Equals(b);
    // Хэш-код по Id — согласован с Equals (равные Id -> равные хэши); без этого Dictionary/HashSet работали бы неверно
    public override int GetHashCode() => Id.GetHashCode();

    // Перегруженные операторы: позволяют писать band1 > band2. Все опираются на CompareTo (порядок: альбомы, затем DisplayName). В UI напрямую не вызываются — демонстрация перегрузки
    public static bool operator >(MusicBand left, MusicBand right) => left.CompareTo(right) > 0;
    // оператор «меньше»
    public static bool operator <(MusicBand left, MusicBand right) => left.CompareTo(right) < 0;
    // оператор «больше или равно»
    public static bool operator >=(MusicBand left, MusicBand right) => left.CompareTo(right) >= 0;
    // оператор «меньше или равно»
    public static bool operator <=(MusicBand left, MusicBand right) => left.CompareTo(right) <= 0;
    // оператор ==: через статический Equals(object, object) — безопасен к null и сравнивает по Id, а не по ссылке
    public static bool operator ==(MusicBand? left, MusicBand? right) => Equals(left, right);
    // оператор != — отрицание ==
    public static bool operator !=(MusicBand? left, MusicBand? right) => !Equals(left, right);

    // Переопределение object.ToString: базовая строка "ТипКласса #Id: ..." (GetType().Name даёт RockBand или PopBand — реальный тип объекта).
    // Наследники вызывают base.ToString() и дописывают своё. Используется: Console.WriteLine(b) в ConsoleApp.ListAll (пункт 1), SearchInteractive (пункт 5), AddBandInteractive («Добавлено: ...»)
    public override string ToString()
        => $"{GetType().Name} #{Id}: {DisplayName}, Лидер: {LeadArtist}, Альбомов: {AlbumCount}";
} // конец класса MusicBand
