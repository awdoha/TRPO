// Пространство имён: все классы проекта (Program, MainForm, ParametersForm, Shape, DrawingPanel) лежат в Lab3.
namespace Lab3;

// Класс точки входа. Используется: CLR запускает его метод Main при старте exe (dotnet run / F5).
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    // [STAThread] — однопоточная модель COM (STA); обязательна для окон WinForms. Используется: помечает Main.
    [STAThread]
    static void Main() // Используется: вызывается системой один раз при запуске программы.
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // Включает визуальные стили, DPI и шрифт по умолчанию (код генерируется из настроек в Lab3.csproj).
        ApplicationConfiguration.Initialize();
        // Создаёт главное окно MainForm и запускает цикл сообщений; программа живёт, пока окно не закрыто.
        Application.Run(new MainForm());
    } // конец метода Main
} // конец класса Program
