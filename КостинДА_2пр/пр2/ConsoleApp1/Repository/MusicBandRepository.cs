using MusicCatalog.Domain; // нужен тип MusicBand. Используется: везде в этом файле

namespace MusicCatalog.Repository; // пространство имён репозитория; подключается в UI/ConsoleApp.cs и Storage/RepositoryFileStore.cs

// Репозиторий = «хранилище групп в памяти» с операциями добавить/удалить/найти/сортировать/посчитать.
// Используется: поле _repo в ConsoleApp (все пункты меню), RepositoryFileStore.Save (читает) и Load (создаёт новый)
public class MusicBandRepository
{
    // Внутренний список групп. private readonly = снаружи не виден, ссылку на список менять нельзя (содержимое — можно). `[]` — collection expression: короткая запись пустого списка (как new List<MusicBand>()).
    // Используется: во всех методах ниже
    private readonly List<MusicBand> _bands = [];

    // Сколько групп в репозитории. Используется: ConsoleApp.SeedIfEmpty (проверка «пусто ли»)
    public int Count => _bands.Count;

    // Индексатор по позиции
    // Индексатор = «свойство с квадратными скобками»: repo[0] вернёт первую группу. Используется: в приложении не вызывается (демонстрация индексаторов)
    public MusicBand this[int index] => _bands[index];

    // Индексатор по названию группы
    // Второй индексатор (перегрузка по типу параметра): repo["Queen"]. Ищет первую группу с таким названием без учёта регистра;
    // `??` — если FirstOrDefault вернул null (не нашёл), то throw выбрасывает KeyNotFoundException. Используется: в приложении не вызывается (демонстрация)
    public MusicBand this[string name]
        => _bands.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
           ?? throw new KeyNotFoundException($"Группа с названием '{name}' не найдена.");

    // Возвращает список только для чтения (AsReadOnly — обёртка: снаружи нельзя добавлять/удалять, только смотреть). Используется: ConsoleApp.ListAll (пункт 1), ConsoleApp.LoadInteractive (пункт 9), RepositoryFileStore.Save (пункт 8)
    public IReadOnlyList<MusicBand> GetAll() => _bands.AsReadOnly();

    // Добавляет группу в список. Используется: ConsoleApp.SeedIfEmpty, AddBandInteractive (пункт 2), LoadInteractive (пункт 9), RepositoryFileStore.Load
    public void Add(MusicBand band)
    {
        // Защита от null
        if (band is null) throw new ArgumentNullException(nameof(band));
        // Any(лямбда) = «есть ли хоть один элемент, для которого условие истинно»; лямбда b => b.Id == band.Id: для каждой группы b сравниваем Id. Дубликат Id запрещён
        if (_bands.Any(b => b.Id == band.Id))
            // Сообщение попадёт в «Ошибка: ...» из try/catch в ConsoleApp.Run
            throw new InvalidOperationException($"Группа с Id={band.Id} уже существует.");
        // Все проверки пройдены — добавляем в конец списка
        _bands.Add(band);
    } // конец метода Add

    // Удаляет группу по Id; true — удалили, false — не нашли. Используется: ConsoleApp.RemoveInteractive (пункт 3)
    public bool RemoveById(int id)
    {
        // FindIndex возвращает позицию первого подходящего элемента (лямбда — условие поиска) или -1
        int idx = _bands.FindIndex(b => b.Id == id);
        // -1 = не найдено -> false (UI напечатает «Не найдено.»)
        if (idx < 0) return false;
        // Удаляем элемент по позиции
        _bands.RemoveAt(idx);
        // Сообщаем об успехе (UI напечатает «Удалено.»)
        return true;
    } // конец метода RemoveById

    // Удаляет конкретный объект группы. Используется: в приложении не вызывается (запасной вариант API)
    public bool Remove(MusicBand band)
    {
        // null нечего удалять
        if (band is null) return false;
        // List.Remove ищет элемент через Equals (у MusicBand он сравнивает по Id) и возвращает true/false
        return _bands.Remove(band);
    } // конец метода Remove

    // Сортировка по внешнему компаратору (паттерн «Стратегия»: способ сортировки передаётся снаружи). Используется: ConsoleApp.SortInteractive (пункт 4, выбор 2) с BandByNameComparer.Instance
    public void SortBy(IComparer<MusicBand> comparer) => _bands.Sort(comparer);

    // Сортировка «по умолчанию»: List.Sort() без аргументов использует IComparable<MusicBand>.CompareTo из MusicBand (альбомы, затем имя). Используется: ConsoleApp.SortInteractive (пункт 4, выбор 1)
    public void SortByDefault() => _bands.Sort();

    // Подсчёт количества объектов каждого конкретного типа
    // Возвращает словарь «имя типа -> количество». LINQ: GroupBy группирует группы по имени реального типа (RockBand/PopBand), ToDictionary превращает каждую группу в пару: ключ g.Key, значение g.Count().
    // Регистр ключа не важен (OrdinalIgnoreCase). Используется: ConsoleApp.ShowCounts (пункт 6)
    public Dictionary<string, int> CountByType()
        => _bands
            // группируем по реальному типу объекта (GetType().Name — «RockBand» или «PopBand»)
            .GroupBy(b => b.GetType().Name)
            // g.Key — имя типа, g.Count() — сколько групп в этой корзине
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

    // Ищет группу по Id; `?` в MusicBand? = может вернуть null. FirstOrDefault — первый подходящий или null. Используется: в приложении не вызывается
    public MusicBand? FindById(int id) => _bands.FirstOrDefault(b => b.Id == id);

    // Ищет группу по названию без учёта регистра; null, если нет. Используется: в приложении не вызывается (в отличие от индексатора не бросает исключение)
    public MusicBand? FindByName(string name)
        => _bands.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    // Поиск с несколькими параметрами
    // Необязательные параметры (= null): можно задать любые из name/genre/minAlbums. Возвращает IEnumerable — LINQ-запрос с ОТЛОЖЕННЫМ выполнением (реально считается при foreach в вызывающем коде).
    // Используется: ConsoleApp.SearchInteractive (пункт 5)
    public IEnumerable<MusicBand> Search(string? name = null, string? genre = null, int? minAlbums = null)
    {
        // Стартуем со всего списка; дальше сужаем цепочкой Where (каждый Where добавляет условие «И»)
        IEnumerable<MusicBand> q = _bands;

        // Если название задано — оставляем группы, в имени которых есть подстрока (Contains, без учёта регистра)
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(b => b.Name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase));

        // Если жанр задан — фильтр по подстроке в жанре
        if (!string.IsNullOrWhiteSpace(genre))
            q = q.Where(b => b.Genre.Contains(genre.Trim(), StringComparison.OrdinalIgnoreCase));

        // int? = число или null; HasValue — «значение задано». Тогда оставляем группы с числом альбомов не меньше минимума (Value — само число)
        if (minAlbums.HasValue)
            q = q.Where(b => b.AlbumCount >= minAlbums.Value);

        // Возвращаем построенный запрос; он выполнится при переборе в ConsoleApp.SearchInteractive
        return q;
    } // конец метода Search

    // Универсальная фильтрация через предикат
    // Func<MusicBand, bool> — делегат: «функция, принимающая группу и возвращающая bool». Вызывающий сам задаёт условие лямбдой. Используется: в приложении не вызывается (демонстрация делегатов)
    public IEnumerable<MusicBand> Filter(Func<MusicBand, bool> predicate)
    {
        // Без условия фильтровать нечем
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        // Where применяет переданное условие к каждой группе; выполнение отложенное
        return _bands.Where(predicate);
    } // конец метода Filter

    // Считает статистику. Используется: ConsoleApp.ShowStats (пункт 7)
    public MusicBandRepositoryStats GetStats()
    {
        // Общее число групп
        int total = _bands.Count;
        // Среднее число альбомов; Average(лямбда) усредняет b.AlbumCount по всем группам; при пустом списке Average бросил бы исключение, поэтому тернарный оператор даёт 0
        double avgAlbums = total == 0 ? 0 : _bands.Average(b => b.AlbumCount);
        // Средний «стаж» лидера (Artist.Score); та же защита от пустого списка
        double avgArtistScore = total == 0 ? 0 : _bands.Average(b => b.LeadArtist.Score);
        // Упаковываем три числа в неизменяемый record и возвращаем в ConsoleApp.ShowStats
        return new MusicBandRepositoryStats(total, avgAlbums, avgArtistScore);
    } // конец метода GetStats

    // Очищает список. Используется: ConsoleApp.LoadInteractive (пункт 9) перед переносом загруженных групп
    public void Clear() => _bands.Clear();
} // конец класса MusicBandRepository

// record = компактный неизменяемый класс «только данные»: компилятор сам создаёт конструктор, свойства (Total, AverageAlbumCount, AverageArtistScore), Equals и ToString. sealed — наследоваться нельзя.
// Используется: возвращаемое значение GetStats; читается в ConsoleApp.ShowStats (s.Total, s.AverageAlbumCount, s.AverageArtistScore)
public sealed record MusicBandRepositoryStats(int Total, double AverageAlbumCount, double AverageArtistScore);
