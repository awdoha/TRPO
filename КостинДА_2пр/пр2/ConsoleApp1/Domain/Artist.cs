namespace MusicCatalog.Domain; // пространство имён предметной области (модель данных); подключается в Repository, Storage, UI

// Артист (лидер группы). IComparable<Artist> = «умею сравниваться с другим артистом» (даёт CompareTo и сортировку);
// IEquatable<Artist> = «умею проверять равенство с другим артистом» (даёт типизированный Equals). Используется: как LeadArtist в MusicBand; создаётся в ConsoleApp.SeedIfEmpty, ConsoleApp.AddBandInteractive и RepositoryFileStore.Load
public class Artist : IComparable<Artist>, IEquatable<Artist>
{
    // Имя артиста; только get (значение задаётся один раз в конструкторе — неизменяемое). Используется: ToString, Key, RepositoryFileStore.Save (ArtistDto.Name)
    public string Name { get; }
    // Инструмент артиста; только get. Используется: ToString и RepositoryFileStore.Save (ArtistDto.Instrument)
    public string Instrument { get; }
    // Год рождения; только get. Используется: Key, Score, ToString, RepositoryFileStore.Save
    public int BirthYear { get; }

    // Статический «артист по умолчанию» (Unknown, 1980); создаётся один раз при первом обращении к классу. Используется: в коде приложения нигде не вызывается (запасной вариант API)
    public static Artist Default { get; } = new("Unknown", "Unknown", 1980);

    // Конструктор: создаёт артиста и проверяет данные. Используется: ConsoleApp.SeedIfEmpty, ConsoleApp.AddBandInteractive (пункт 2), RepositoryFileStore.Load (пункт 9), а также свойство Default
    public Artist(string name, string instrument, int birthYear)
    {
        // Убираем пробелы по краям; `name ?? string.Empty` — если name равен null, берём пустую строку (защита от null)
        Name = (name ?? string.Empty).Trim();
        // То же для инструмента
        Instrument = (instrument ?? string.Empty).Trim();

        // Если имя пустое или из пробелов — бросаем исключение; его ловит try/catch в ConsoleApp.Run и печатает «Ошибка: ...»
        if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("Name is required.");
        // Аналогичная проверка инструмента
        if (string.IsNullOrWhiteSpace(Instrument)) throw new ArgumentException("Instrument is required.");
        // Год рождения должен быть от 1900 до текущего года (DateTime.Now.Year — текущий год)
        if (birthYear < 1900 || birthYear > DateTime.Now.Year)
            // nameof(birthYear) даёт строку "birthYear" — имя параметра, из-за которого ошибка; исключение уйдёт в try/catch в ConsoleApp.Run
            throw new ArgumentOutOfRangeException(nameof(birthYear), "BirthYear must be between 1900 and current year.");

        // Год прошёл проверку — сохраняем в свойство (присвоить get-only свойство можно только в конструкторе)
        BirthYear = birthYear;
    } // конец конструктора Artist

    // Ключ для дедупликации при сохранении/загрузке
    // Уникальный ключ артиста "ИМЯ|ГОД" в верхнем регистре; virtual = наследники могли бы переопределить. Используется: CompareTo, Equals, GetHashCode-> ключ равенства, RepositoryFileStore.Save (ArtistDto.Id, словарь артистов) и ToDto (ArtistId)
    public virtual string Key => $"{Name}|{BirthYear}".ToUpperInvariant();

    // Стаж (лет с рождения) — используется при сравнении
    // Считаемое свойство «стаж»: текущий год минус год рождения. Используется: CompareTo (основной ключ сравнения) и MusicBandRepository.GetStats (средний стаж, пункт меню 7)
    public double Score => DateTime.Now.Year - BirthYear;

    // Реализация IComparable<Artist>: сравнивает this с other (<0 меньше, 0 равно, >0 больше). Используется: операторы >, <, >=, <= ниже
    public int CompareTo(Artist? other)
    {
        // Любой артист «больше» null (по правилам .NET)
        if (other is null) return 1;
        // Сначала сравниваем по стажу
        int byScore = Score.CompareTo(other.Score);
        // Если стаж разный — этого достаточно, возвращаем результат
        if (byScore != 0) return byScore;
        // Стаж равен — сравниваем по Key (побуквенно, Ordinal), чтобы порядок был однозначным
        return string.Compare(Key, other.Key, StringComparison.Ordinal);
    } // конец метода CompareTo

    // Реализация IEquatable<Artist>: равны, если other не null и Key совпадает. Используется: Equals(object) ниже и оператор ==
    public bool Equals(Artist? other) => other is not null && Key == other.Key;
    // Переопределение object.Equals; `obj is Artist a` — проверка типа и приведение сразу; делегирует типизированному Equals. Используется: оператор ==, коллекции
    public override bool Equals(object? obj) => obj is Artist a && Equals(a);
    // Хэш-код обязан быть согласован с Equals: равные артисты (одинаковый Key) дают одинаковый хэш. Используется: Dictionary/HashSet, если артист станет ключом
    public override int GetHashCode() => Key.GetHashCode();

    // Перегруженные операторы сравнения: позволяют писать a1 > a2. Все делегируют CompareTo. Используются: в приложении напрямую не вызываются (демонстрация перегрузки операторов)
    public static bool operator >(Artist left, Artist right) => left.CompareTo(right) > 0;
    // оператор «меньше»: пара к «больше» (компилятор требует перегружать парами)
    public static bool operator <(Artist left, Artist right) => left.CompareTo(right) < 0;
    // оператор «больше или равно»
    public static bool operator >=(Artist left, Artist right) => left.CompareTo(right) >= 0;
    // оператор «меньше или равно»
    public static bool operator <=(Artist left, Artist right) => left.CompareTo(right) <= 0;
    // оператор ==: сравнивает по значению (через Equals, безопасно к null), а не по ссылке
    public static bool operator ==(Artist? left, Artist? right) => Equals(left, right);
    // оператор != — отрицание ==; == и != тоже перегружаются парой
    public static bool operator !=(Artist? left, Artist? right) => !Equals(left, right);

    // Текстовое представление артиста. Используется: в MusicBand.ToString (часть «Лидер: ...») -> вывод пункта меню 1, 5 и сообщения «Добавлено»
    public override string ToString() => $"{Name} ({Instrument}, г.р. {BirthYear})";
} // конец класса Artist
