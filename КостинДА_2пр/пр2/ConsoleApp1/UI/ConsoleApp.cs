using MusicCatalog.Domain; // Artist, MusicBand, RockBand, PopBand. Используется: SeedIfEmpty, AddBandInteractive
using MusicCatalog.Repository; // MusicBandRepository. Используется: поле _repo
using MusicCatalog.Repository.Comparers; // BandByNameComparer. Используется: SortInteractive
using MusicCatalog.Storage; // RepositoryFileStore. Используется: SaveInteractive, LoadInteractive

namespace MusicCatalog.UI; // пространство имён пользовательского интерфейса; подключается в Program.cs

// Консольное меню. sealed = наследоваться нельзя. Используется: Program.Main создаёт объект и вызывает Run
public sealed class ConsoleApp
{
    // Репозиторий групп — единственное хранилище данных программы; readonly = ссылку заменить нельзя, `new()` — создаём сразу. Используется: во всех методах меню
    private readonly MusicBandRepository _repo = new();

    // Главный цикл меню. Используется: Program.Main (app.Run())
    public void Run()
    {
        // При первом запуске заполняем репозиторий тремя демонстрационными группами
        SeedIfEmpty();

        // Бесконечный цикл: выходит только return при вводе 0
        while (true)
        {
            // Печатаем меню (PrintMenu)
            PrintMenu();
            // Приглашение к вводу (Write — без перевода строки)
            Console.Write("Выберите пункт: ");
            // ReadLine читает строку (null при конце ввода -> `??` даёт пустую строку); Trim убирает пробелы. Результат идёт в switch
            string choice = (Console.ReadLine() ?? string.Empty).Trim();

            // try/catch: любая ошибка в пункте меню (неверное число, пустое имя и т.п.) не роняет программу
            try
            {
                // Выбираем действие по введённой строке
                switch (choice)
                {
                    // пункт 1 -> показать все группы
                    case "1": ListAll(); break;
                    // пункт 2 -> добавить группу
                    case "2": AddBandInteractive(); break;
                    // пункт 3 -> удалить по Id
                    case "3": RemoveInteractive(); break;
                    // пункт 4 -> сортировка
                    case "4": SortInteractive(); break;
                    // пункт 5 -> поиск/фильтрация
                    case "5": SearchInteractive(); break;
                    // пункт 6 -> подсчёт по типам
                    case "6": ShowCounts(); break;
                    // пункт 7 -> статистика
                    case "7": ShowStats(); break;
                    // пункт 8 -> сохранить в JSON
                    case "8": SaveInteractive(); break;
                    // пункт 9 -> загрузить из JSON
                    case "9": LoadInteractive(); break;
                    // пункт 0 -> выход: return завершает Run, затем Main, программа заканчивается
                    case "0": return;
                    // любой другой ввод
                    default: Console.WriteLine("Неизвестная команда."); break;
                }
            }
            // ловим любое исключение (из конструкторов с проверками, int.Parse, чтения файла) в переменную ex
            catch (Exception ex)
            {
                // показываем текст ошибки и возвращаемся к меню
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            // Пустая строка между итерациями меню
            Console.WriteLine();
        }
    } // конец метода Run

    // Печатает пункты меню. static — не использует поля объекта. Используется: Run (в начале каждой итерации цикла)
    private static void PrintMenu()
    {
        // заголовок
        Console.WriteLine("Меню:");
        // пункт 1 — вызывает ListAll
        Console.WriteLine("1) Показать все группы");
        // пункт 2 — вызывает AddBandInteractive
        Console.WriteLine("2) Добавить группу");
        // пункт 3 — вызывает RemoveInteractive
        Console.WriteLine("3) Удалить группу по Id");
        // пункт 4 — вызывает SortInteractive
        Console.WriteLine("4) Сортировка");
        // пункт 5 — вызывает SearchInteractive
        Console.WriteLine("5) Поиск/фильтрация");
        // пункт 6 — вызывает ShowCounts
        Console.WriteLine("6) Подсчёт по типам");
        // пункт 7 — вызывает ShowStats
        Console.WriteLine("7) Статистика");
        // пункт 8 — вызывает SaveInteractive
        Console.WriteLine("8) Сохранить в файл");
        // пункт 9 — вызывает LoadInteractive
        Console.WriteLine("9) Загрузить из файла");
        // пункт 0 — выход из цикла в Run
        Console.WriteLine("0) Выход");
    } // конец метода PrintMenu

    // Заполняет репозиторий тремя демо-группами. Используется: Run (один раз при старте)
    private void SeedIfEmpty()
    {
        // Если данные уже есть — ничего не делаем
        if (_repo.Count > 0) return;

        // Три артиста-лидера (конструктор Artist проверяет данные); используются ниже как LeadArtist групп
        var freddie = new Artist("Freddie Mercury", "Вокал", 1946);
        // лидер Nirvana
        var kurt    = new Artist("Kurt Cobain", "Гитара/Вокал", 1967);
        // лидер BLACKPINK (по условию демо)
        var taylor  = new Artist("Taylor Swift", "Гитара/Вокал", 1989);

        // Рок-группа Queen; именованные аргументы (hasLiveAlbums: true) делают вызов читаемым; Add кладёт группу в репозиторий; Id выдастся автоматически (1)
        _repo.Add(new RockBand("Queen", "Рок", 1970, freddie, hasLiveAlbums: true, albumCount: 15));
        // Рок-группа Nirvana (Id 2)
        _repo.Add(new RockBand("Nirvana", "Гранж", 1987, kurt, hasLiveAlbums: true, albumCount: 3));
        // Поп-группа BLACKPINK (Id 3)
        _repo.Add(new PopBand("BLACKPINK", "К-поп", 2016, taylor, isGirlGroup: true, albumCount: 2));
    } // конец метода SeedIfEmpty

    // Пункт 1: выводит все группы. Используется: Run (case "1")
    private void ListAll()
    {
        // Получаем список групп (только чтение)
        var all = _repo.GetAll();
        // Если пусто — сообщение и выход из метода
        if (all.Count == 0) { Console.WriteLine("Репозиторий пуст."); return; }
        // Console.WriteLine(b) вызывает b.ToString() — сработает override у RockBand/PopBand (полиморфизм)
        foreach (var b in all) Console.WriteLine(b);
    } // конец метода ListAll

    // Пункт 2: запрашивает данные и добавляет группу. Используется: Run (case "2")
    private void AddBandInteractive()
    {
        // спрашиваем тип
        Console.Write("Тип (1=RockBand, 2=PopBand): ");
        // читаем ответ; сохраняем в t (проверяется ниже: "1" — рок, иначе поп)
        string t = (Console.ReadLine() ?? string.Empty).Trim();

        // спрашиваем название
        Console.Write("Название группы: ");
        // читаем название -> уйдёт в конструктор группы
        string name = (Console.ReadLine() ?? string.Empty).Trim();

        // спрашиваем жанр
        Console.Write("Жанр: ");
        // читаем жанр -> в конструктор группы
        string genre = (Console.ReadLine() ?? string.Empty).Trim();

        // спрашиваем год основания
        Console.Write("Год основания: ");
        // int.Parse превращает строку в число (при нечисловом вводе — FormatException, его поймает try/catch в Run)
        int formedYear = int.Parse(Console.ReadLine() ?? "2000");

        // спрашиваем имя лидера
        Console.Write("Имя лидер-артиста: ");
        // читаем имя лидера -> в new Artist
        string artistName = (Console.ReadLine() ?? string.Empty).Trim();

        // спрашиваем инструмент
        Console.Write("Инструмент лидера: ");
        // читаем инструмент -> в new Artist
        string instrument = (Console.ReadLine() ?? string.Empty).Trim();

        // спрашиваем год рождения
        Console.Write("Год рождения лидера: ");
        // читаем и разбираем число -> в new Artist
        int birthYear = int.Parse(Console.ReadLine() ?? "1980");

        // Создаём артиста (здесь выполнятся проверки данных); объект пойдёт как LeadArtist в группу
        var artist = new Artist(artistName, instrument, birthYear);

        // спрашиваем число альбомов
        Console.Write("Количество альбомов: ");
        // читаем и разбираем число -> в конструктор группы
        int albums = int.Parse(Console.ReadLine() ?? "0");

        // Переменная базового типа: в неё положим либо RockBand, либо PopBand (полиморфизм: тип переменной — предок)
        MusicBand band;
        // Если выбрали "1" — рок-группа
        if (t == "1")
        {
            // спрашиваем про концертные альбомы
            Console.Write("Есть концертные альбомы? (y/n): ");
            // `is "y" or "yes" or "д" or "да"` — pattern matching: проверка «равно одному из значений»; результат true/false
            bool live = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant() is "y" or "yes" or "д" or "да";
            // создаём рок-группу (Id выдастся автоматически)
            band = new RockBand(name, genre, formedYear, artist, live, albums);
        }
        // иначе — поп-группа
        else
        {
            // спрашиваем про женский состав
            Console.Write("Женская группа? (y/n): ");
            // та же проверка ответа
            bool girl = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant() is "y" or "yes" or "д" or "да";
            // создаём поп-группу
            band = new PopBand(name, genre, formedYear, artist, girl, albums);
        }

        // Добавляем в репозиторий (если Id дублируется — исключение)
        _repo.Add(band);
        // Строка + объект: band автоматически превращается в текст через ToString
        Console.WriteLine("Добавлено: " + band);
    } // конец метода AddBandInteractive

    // Пункт 3: удаляет группу по Id. Используется: Run (case "3")
    private void RemoveInteractive()
    {
        // спрашиваем Id
        Console.Write("Id для удаления: ");
        // разбираем число (Id можно посмотреть в пункте 1)
        int id = int.Parse(Console.ReadLine() ?? "0");
        // RemoveById вернёт true/false; тернарный оператор выбирает, какое сообщение напечатать
        Console.WriteLine(_repo.RemoveById(id) ? "Удалено." : "Не найдено.");
    } // конец метода RemoveInteractive

    // Пункт 4: сортирует список групп. Используется: Run (case "4")
    private void SortInteractive()
    {
        // подсказка вариантов
        Console.WriteLine("Сортировка: 1=по умолчанию (кол-во альбомов), 2=по названию");
        // приглашение
        Console.Write("Выбор: ");
        // читаем выбор
        string s = (Console.ReadLine() ?? string.Empty).Trim();
        // "2" -> сортировка по внешнему компаратору (стратегия по названию)
        if (s == "2") _repo.SortBy(BandByNameComparer.Instance);
        // иначе — по умолчанию через IComparable MusicBand (альбомы, затем название)
        else _repo.SortByDefault();
        // подтверждение; результат виден в пункте 1
        Console.WriteLine("Отсортировано.");
    } // конец метода SortInteractive

    // Пункт 5: поиск по названию/жанру/минимуму альбомов. Используется: Run (case "5")
    private void SearchInteractive()
    {
        // спрашиваем часть названия
        Console.Write("Название (или пусто): ");
        // читаем; пусто = не фильтровать
        string name = (Console.ReadLine() ?? string.Empty).Trim();
        // спрашиваем жанр
        Console.Write("Жанр (или пусто): ");
        // читаем; пусто = не фильтровать
        string genre = (Console.ReadLine() ?? string.Empty).Trim();
        // спрашиваем минимум альбомов
        Console.Write("Мин. кол-во альбомов (или пусто): ");
        // читаем как строку (число разберём ниже)
        string ma = (Console.ReadLine() ?? string.Empty).Trim();

        // int? = «число или null»: пусто -> null (не фильтровать), иначе число из строки
        int? minAlbums = string.IsNullOrWhiteSpace(ma) ? null : int.Parse(ma);
        // Вызываем Search с именованными аргументами; пустые строки заменяем на null, чтобы Search не применял этот фильтр
        var results = _repo.Search(
            name: string.IsNullOrWhiteSpace(name) ? null : name,
            genre: string.IsNullOrWhiteSpace(genre) ? null : genre,
            minAlbums: minAlbums);

        // Только здесь запрос реально выполняется (отложенное выполнение LINQ); каждая найденная группа печатается через ToString
        foreach (var b in results) Console.WriteLine(b);
    } // конец метода SearchInteractive

    // Пункт 6: сколько групп каждого типа. Используется: Run (case "6")
    private void ShowCounts()
    {
        // Словарь «тип -> количество» из MusicBandRepository.CountByType (LINQ GroupBy)
        var map = _repo.CountByType();
        // Пустой словарь — сообщение и выход
        if (map.Count == 0) { Console.WriteLine("Репозиторий пуст."); return; }
        // kv — пара ключ/значение (KeyValuePair): Key = имя типа, Value = число групп
        foreach (var kv in map) Console.WriteLine($"{kv.Key}: {kv.Value}");
    } // конец метода ShowCounts

    // Пункт 7: печатает статистику. Используется: Run (case "7")
    private void ShowStats()
    {
        // Получаем record MusicBandRepositoryStats (общее число, среднее альбомов, средний стаж)
        var s = _repo.GetStats();
        // всего групп
        Console.WriteLine($"Всего: {s.Total}");
        // формат :0.## — до двух знаков после запятой, без лишних нулей
        Console.WriteLine($"Среднее кол-во альбомов: {s.AverageAlbumCount:0.##}");
        // формат :0.# — один знак после запятой
        Console.WriteLine($"Средний стаж лидер-артиста: {s.AverageArtistScore:0.#} лет");
    } // конец метода ShowStats

    // Пункт 8: сохраняет репозиторий в JSON-файл. Используется: Run (case "8")
    private void SaveInteractive()
    {
        // спрашиваем путь
        Console.Write("Путь к файлу (например data.json): ");
        // читаем путь (относительный путь — от текущей папки запуска)
        string path = (Console.ReadLine() ?? "data.json").Trim();
        // Записываем в файл (RepositoryFileStore.Save превращает группы в DTO и JSON)
        RepositoryFileStore.Save(path, _repo);
        // подтверждение
        Console.WriteLine("Сохранено.");
    } // конец метода SaveInteractive

    // Пункт 9: загружает репозиторий из JSON-файла. Используется: Run (case "9")
    private void LoadInteractive()
    {
        // спрашиваем путь
        Console.Write("Путь к файлу: ");
        // читаем путь
        string path = (Console.ReadLine() ?? "data.json").Trim();
        // Load читает файл и возвращает новый репозиторий; при ошибке (нет файла) исключение уйдёт в Run и будет показано как «Ошибка: ...»
        var loaded = RepositoryFileStore.Load(path);

        // Только после успешной загрузки очищаем текущие данные (чтобы при ошибке ничего не потерять)
        _repo.Clear();
        // переносим все группы из загруженного репозитория в наш _repo
        foreach (var b in loaded.GetAll())
            _repo.Add(b);

        // подтверждение
        Console.WriteLine("Загружено.");
    } // конец метода LoadInteractive
} // конец класса ConsoleApp
