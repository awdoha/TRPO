using MusicCatalog.Domain; // нужен тип MusicBand. Используется: в IComparer<MusicBand> и в сигнатуре Compare

namespace MusicCatalog.Repository.Comparers; // пространство имён компараторов; подключается в UI/ConsoleApp.cs (using ...Comparers)

// Компаратор (стратегия сортировки) «по названию». IComparer<MusicBand> = «внешний судья»: умеет сравнить ДВЕ группы, не изменяя сам класс MusicBand.
// sealed = от класса нельзя наследоваться. Используется: ConsoleApp.SortInteractive (пункт меню 4 -> выбор 2) передаёт Instance в MusicBandRepository.SortBy
public sealed class BandByNameComparer : IComparer<MusicBand>
{
    // Единственный общий экземпляр (паттерн «одиночка»): у компаратора нет состояния, поэтому new каждый раз не нужен. Используется: ConsoleApp.SortInteractive
    public static BandByNameComparer Instance { get; } = new();

    // Приватный конструктор: снаружи new BandByNameComparer() написать нельзя — только через Instance. Вызывается один раз, при создании Instance
    private BandByNameComparer() { }

    // Метод интерфейса IComparer: <0 если x «меньше» y, 0 если равны, >0 если x «больше». Вызывается List.Sort внутри MusicBandRepository.SortBy
    public int Compare(MusicBand? x, MusicBand? y)
    {
        // Один и тот же объект (или оба null) — считаем равными
        if (ReferenceEquals(x, y)) return 0;
        // null считаем «меньше» любого объекта, чтобы он оказался в начале
        if (x is null) return -1;
        // если null только y — x «больше»
        if (y is null) return 1;
        // Основное сравнение: по DisplayName ("Имя (Жанр)") без учёта регистра (OrdinalIgnoreCase). DisplayName определён в MusicBand
        return string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
    } // конец метода Compare
} // конец класса BandByNameComparer
