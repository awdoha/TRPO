// Пространство имён проекта (file-scoped: действует на весь файл). Используется: общее для всех файлов Lab3.
namespace Lab3;

/// <summary>
/// Способ отображения набора точек: обычные точки без соединения,
/// замкнутая кривая, многоугольник, кривые Безье или залитая кривая.
/// </summary>
// enum — набор именованных констант. Используется: в MainForm поле _currentMode, метод SetMode,
// switch (mode) в DrawShape и параметр конструктора Shape.
public enum ShapeMode
{
    None, // только точки, без линий. Используется: значение _currentMode по умолчанию (пока не нажата кнопка режима)
    Curve, // замкнутая кривая (DrawClosedCurve). Используется: кнопка «Кривая» -> SetMode(ShapeMode.Curve)
    Polygon, // ломаная/многоугольник (DrawPolygon). Используется: кнопка «Ломаная»
    Bezier, // кривая Безье (DrawBeziers). Используется: кнопка «Безье»
    Filled // залитая кривая (FillClosedCurve). Используется: кнопка «Заполненная»
} // конец перечисления ShapeMode

/// <summary>
/// Зафиксированная (сохранённая, см. функцию Р1) фигура: набор точек вместе
/// с тем режимом отображения и цветами, которые действовали в момент сохранения.
/// Собственная копия списка точек нужна, чтобы дальнейшее рисование новых точек
/// в текущей фигуре никак не затрагивало уже сохранённые фигуры.
/// </summary>
// [Р1] Модель сохранённой фигуры. Используется: MainForm.SaveCurrentShape создаёт объекты (кнопка «Сохранить»),
// список _savedShapes их хранит, DrawPanel_Paint рисует, а перетаскивание/движение/стрелки меняют их Points.
public sealed class Shape
{
    // Точки фигуры (собственная копия). get-only: сам список не подменить, но его элементы менять можно.
    // Используется: DrawPanel_Paint (рисование), TryFindPointAt (поиск точки под мышью), AddMovingGroup (движение), ShiftEverything (стрелки).
    public List<Point> Points { get; }
    // Режим отображения на момент сохранения. Используется: DrawPanel_Paint передаёт его в DrawShape.
    public ShapeMode Mode { get; }
    // Цвет точек на момент сохранения. Используется: DrawPanel_Paint -> DrawShape (кисть точек).
    public Color PointColor { get; }
    // Цвет линии на момент сохранения. Используется: DrawPanel_Paint -> DrawShape (перо и заливка).
    public Color LineColor { get; }

    // Конструктор. Используется: единственный вызов — MainForm.SaveCurrentShape (нажатие «Сохранить», Р1).
    public Shape(IEnumerable<Point> points, ShapeMode mode, Color pointColor, Color lineColor)
    {
        // [Р1] new List<Point>(points) копирует элементы, поэтому потом _currentPoints.Clear() не стирает сохранённую фигуру.
        Points = new List<Point>(points);
        Mode = mode; // запоминаем режим отображения
        PointColor = pointColor; // запоминаем цвет точек
        LineColor = lineColor; // запоминаем цвет линии
    } // конец конструктора Shape
} // конец класса Shape
