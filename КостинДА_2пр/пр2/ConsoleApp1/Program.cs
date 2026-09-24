using MusicCatalog.UI; // подключаем пространство имён UI, чтобы писать просто ConsoleApp. Используется: в Main (new ConsoleApp())
using MusicCatalog.Utils; // подключаем Utils, чтобы писать VariantCalculator. Используется: в Main (расчёт варианта)

namespace MusicCatalog; // пространство имён всей программы (file-scoped: действует на весь файл). Совпадает с RootNamespace из ConsoleApp1.csproj

// Точка входа. static = нельзя создать объект; internal = виден только внутри этой сборки (MusicCatalog.dll). Используется: запускается средой .NET при старте программы
internal static class Program
{
    // Main — с него начинается выполнение. Вызывается самим .NET при `dotnet run`; аргументы не нужны
    public static void Main()
    {
        // Ставим UTF-8 для консоли, чтобы русский текст меню (ConsoleApp.PrintMenu и др.) не превращался в «кракозябры»
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // Считаем номер варианта по буквам 'К' и 'Д' (фамилия/имя) через VariantCalculator.Compute; результат печатается следующей строкой
        int variant = VariantCalculator.Compute('К', 'Д');
        // Печатаем номер варианта ($"..." — интерполяция: {variant} подставляется в текст). Это первое, что видно в консоли
        Console.WriteLine($"Номер варианта: {variant}");
        // Пустая строка для отступа между номером варианта и меню
        Console.WriteLine();

        // Создаём объект консольного приложения (UI/ConsoleApp.cs); var — тип выводится сам. Внутри него создаётся репозиторий групп
        var app = new ConsoleApp();
        // Запускаем бесконечный цикл меню (ConsoleApp.Run); вернётся только после ввода 0
        app.Run();
    } // конец метода Main
} // конец класса Program
