using System.Text.Json; // JSON-сериализатор .NET (JsonSerializer, JsonSerializerOptions). Используется: Save, Load, JsonOptions
using MusicCatalog.Domain; // Artist, MusicBand, RockBand, PopBand. Используется: Save, Load, ToDto, FromDto
using MusicCatalog.Repository; // MusicBandRepository. Используется: параметры Save и результат Load

namespace MusicCatalog.Storage; // пространство имён хранения; подключается в UI/ConsoleApp.cs

// Статический класс сохранения/загрузки репозитория в JSON-файл. Используется: ConsoleApp.SaveInteractive (пункт 8) -> Save; ConsoleApp.LoadInteractive (пункт 9) -> Load
public static class RepositoryFileStore
{
    // DTO (Data Transfer Object) = простой класс «только данные» для JSON. Нужен, потому что MusicBand абстрактный и имеет get-only свойства/конструкторы с проверками — JSON сам такой объект не восстановит.
    // Корневой объект файла. private sealed = виден только внутри RepositoryFileStore. Используется: Save (создаёт), Load (читает)
    private sealed class RepoDto
    {
        // Список артистов в файле (каждый один раз); `= []` — пустой список по умолчанию. set нужен, чтобы десериализатор мог записать значение. Используется: Save (заполняет), Load (читает)
        public List<ArtistDto> Artists { get; set; } = [];
        // Список групп в файле. Используется: Save (заполняет), Load (читает)
        public List<BandDto> Bands { get; set; } = [];
    } // конец класса RepoDto

    // DTO артиста. Используется: RepoDto.Artists; создаётся в Save, читается в Load
    private sealed class ArtistDto
    {
        // Идентификатор артиста в файле = Artist.Key; на него ссылается BandDto.ArtistId. Используется: Save (пишет), Load (ключ словаря)
        public string Id { get; set; } = string.Empty;
        // Имя артиста. Используется: Save, Load (аргумент new Artist)
        public string Name { get; set; } = string.Empty;
        // Инструмент. Используется: Save, Load
        public string Instrument { get; set; } = string.Empty;
        // Год рождения. Используется: Save, Load
        public int BirthYear { get; set; }
    } // конец класса ArtistDto

    // DTO группы: ОДИН плоский класс для обоих типов групп. Используется: RepoDto.Bands; создаётся в ToDto, читается в FromDto
    private sealed class BandDto
    {
        // Имя типа ("RockBand"/"PopBand") — «метка», по которой FromDto решает, какой класс создавать (ручная полиморфная десериализация). Используется: ToDto (пишет), FromDto (switch)
        public string Type { get; set; } = string.Empty;
        // Id группы; сохраняется, чтобы при загрузке Id не менялись. Используется: ToDto, FromDto
        public int Id { get; set; }
        // Название. Используется: ToDto, FromDto
        public string Name { get; set; } = string.Empty;
        // Жанр. Используется: ToDto, FromDto
        public string Genre { get; set; } = string.Empty;
        // Год основания. Используется: ToDto, FromDto
        public int FormedYear { get; set; }
        // Ссылка на артиста (значение ArtistDto.Id). Используется: ToDto (пишет), Load (ищет артиста в словаре)
        public string ArtistId { get; set; } = string.Empty;
        // Количество альбомов. Используется: ToDto, FromDto
        public int AlbumCount { get; set; }

        // RockBand
        // bool? = nullable: у PopBand это поле пустое (null). Заполняется только для рок-групп. Используется: ToDto, FromDto (nameof(RockBand))
        public bool? HasLiveAlbums { get; set; }

        // PopBand
        // Заполняется только для поп-групп. Используется: ToDto, FromDto (nameof(PopBand))
        public bool? IsGirlGroup { get; set; }
    } // конец класса BandDto

    // Настройки JSON: WriteIndented = красивый вывод с отступами (читаемый файл). static readonly = создаётся один раз. Используется: Save (Serialize) и Load (Deserialize)
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Сохраняет репозиторий в файл. Используется: ConsoleApp.SaveInteractive (пункт 8)
    public static void Save(string path, MusicBandRepository repo)
    {
        // Защита от null
        if (repo is null) throw new ArgumentNullException(nameof(repo));

        // Берём все группы (только для чтения) из репозитория
        var bands = repo.GetAll();

        // Артисты сохраняются один раз, без дублирования
        // Словарь «Key артиста -> артист»: одинаковые артисты (одинаковый Key) склеиваются в одну запись
        var artistMap = new Dictionary<string, Artist>(StringComparer.OrdinalIgnoreCase);
        // Select(b => b.LeadArtist) — LINQ-проекция: из списка групп получаем список их лидеров; перебираем их
        foreach (var a in bands.Select(b => b.LeadArtist))
            // Записываем артиста по ключу (повтор ключа просто перезапишет, дубликатов не будет)
            artistMap[a.Key] = a;

        // Собираем корневой DTO с инициализатором объекта { ... }
        var dto = new RepoDto
        {
            // Артисты из словаря
            Artists = artistMap.Values
                // сортируем по ключу, чтобы файл был стабильным
                .OrderBy(a => a.Key, StringComparer.OrdinalIgnoreCase)
                // каждый Artist превращаем в ArtistDto (проекция)
                .Select(a => new ArtistDto
                {
                    // Id артиста в файле = его Key
                    Id = a.Key,
                    // имя
                    Name = a.Name,
                    // инструмент
                    Instrument = a.Instrument,
                    // год рождения
                    BirthYear = a.BirthYear
                })
                // выполняем запрос и получаем List (LINQ до этого был отложенным)
                .ToList(),

            // Каждую группу превращаем в BandDto методом ToDto (method group: краткая запись лямбды b => ToDto(b))
            Bands = bands.Select(ToDto).ToList()
        };

        // Создаём папку для файла, если её нет; `?? "."` — если пути к папке нет (просто имя файла), берём текущую папку
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        // Превращаем DTO в JSON-текст и записываем в файл (перезаписывая)
        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
    } // конец метода Save

    // Загружает репозиторий из файла и возвращает НОВЫЙ репозиторий. Используется: ConsoleApp.LoadInteractive (пункт 9)
    public static MusicBandRepository Load(string path)
    {
        // Читаем весь текст файла (нет файла — исключение, его покажет ConsoleApp.Run)
        string json = File.ReadAllText(path);
        // Разбираем JSON в RepoDto; `?? new RepoDto()` — если файл содержит null, берём пустой DTO
        var dto = JsonSerializer.Deserialize<RepoDto>(json, JsonOptions) ?? new RepoDto();

        // Строим словарь «Id артиста -> настоящий Artist». ToDictionary(ключ, значение, сравнение): для каждого ArtistDto создаём Artist (конструктор заново проверяет данные)
        var artists = dto.Artists.ToDictionary(
            // ключ словаря — Id из файла
            a => a.Id,
            // значение — новый объект Artist
            a => new Artist(a.Name, a.Instrument, a.BirthYear),
            // ключи без учёта регистра
            StringComparer.OrdinalIgnoreCase);

        // Новый пустой репозиторий, в который добавим группы
        var repo = new MusicBandRepository();
        // Перебираем группы из файла
        foreach (var b in dto.Bands)
        {
            // TryGetValue пытается найти артиста по ArtistId; out var artist — получаем результат сразу; `!` — «если НЕ нашли»
            if (!artists.TryGetValue(b.ArtistId, out var artist))
                // файл повреждён: группа ссылается на несуществующего артиста
                throw new InvalidOperationException($"Неизвестный артист '{b.ArtistId}' в файле.");
            // Превращаем DTO в настоящий RockBand/PopBand (FromDto) и добавляем в репозиторий
            repo.Add(FromDto(b, artist));
        }

        // Возвращаем заполненный репозиторий в ConsoleApp.LoadInteractive
        return repo;
    } // конец метода Load

    // Преобразует MusicBand -> BandDto. Используется: Save (Bands = bands.Select(ToDto))
    private static BandDto ToDto(MusicBand band)
    {
        // Создаём DTO и копируем общие поля
        var dto = new BandDto
        {
            // Реальный тип объекта («RockBand»/«PopBand») — метка для загрузки
            Type = band.GetType().Name,
            // Id группы
            Id = band.Id,
            // название
            Name = band.Name,
            // жанр
            Genre = band.Genre,
            // год основания
            FormedYear = band.FormedYear,
            // ссылка на артиста по его Key
            ArtistId = band.LeadArtist.Key,
            // альбомы (полиморфно: берётся override конкретного типа)
            AlbumCount = band.AlbumCount
        };

        // Pattern matching: `band is RockBand r` — проверка типа и сразу переменная r нужного типа. Для рок-группы пишем HasLiveAlbums, иначе для поп-группы — IsGirlGroup
        if (band is RockBand r) dto.HasLiveAlbums = r.HasLiveAlbums;
        else if (band is PopBand p) dto.IsGirlGroup = p.IsGirlGroup;

        // Возвращаем заполненный DTO в Save
        return dto;
    } // конец метода ToDto

    // Преобразует BandDto -> MusicBand (обратное ToDto). Используется: Load. Тело — switch-выражение: по строке dto.Type выбирает, что создать
    private static MusicBand FromDto(BandDto dto, Artist artist)
        => dto.Type switch
        {
            // nameof(RockBand) = "RockBand"; `??` false — если поле null, считаем «нет»; dto.Id передаём, чтобы сохранить прежний Id
            nameof(RockBand) => new RockBand(dto.Name, dto.Genre, dto.FormedYear, artist, dto.HasLiveAlbums ?? false, dto.AlbumCount, dto.Id),
            // для "PopBand" создаём PopBand
            nameof(PopBand)  => new PopBand(dto.Name, dto.Genre, dto.FormedYear, artist, dto.IsGirlGroup ?? false, dto.AlbumCount, dto.Id),
            // `_` = любой другой Type: запасной вариант — PopBand по умолчанию
            _ => new PopBand(dto.Name, dto.Genre, dto.FormedYear, artist, false, dto.AlbumCount, dto.Id)
        }; // конец switch-выражения и метода FromDto
} // конец класса RepositoryFileStore
