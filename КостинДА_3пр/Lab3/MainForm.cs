// Пространство имён System.Drawing.Drawing2D. Используется: SmoothingMode.AntiAlias в DrawPanel_Paint (сглаживание линий).
using System.Drawing.Drawing2D;

// Пространство имён проекта. Используется: общее для всех файлов Lab3.
namespace Lab3;

/// <summary>
/// Главная форма практики 3 «Работа с 2D-графикой в WinForms».
/// Вариант 3: функции Д4 (движение со сменой направления всех точек при столкновении),
/// П2 (настройка цветов через ComboBox), С1 (сдвиг фигуры стрелками) и Р1 (сохранение фигур).
/// </summary>
// Главное окно. Используется: Program.Main -> Application.Run(new MainForm()). sealed — наследовать нельзя; : Form — это окно WinForms.
public sealed class MainForm : Form
{
    // --- Параметры сдвигов и скорости движения ---
    // [С1] Шаг сдвига фигуры стрелкой, пиксели. Используется: ProcessCmdKey (4 стрелки -> ShiftEverything).
    private const int ArrowShiftStep = 8;
    // Радиус кружка точки при рисовании. Используется: DrawShape (FillEllipse) и MoveTimer_Tick (отступ от края).
    private const int PointRadius = 4;
    // Радиус «попадания» мышью по точке (чуть больше видимого, чтобы легче попасть). Используется: TryFindPointIn.
    private const int PointHitRadius = 7;
    // Шаг изменения скорости на одно нажатие «+»/«-». Используется: MainForm_KeyDown (ChangeSpeed).
    private const double SpeedStep = 0.25;
    // Нижняя граница множителя скорости. Используется: ChangeSpeed (Math.Clamp).
    private const double MinSpeedFactor = 0.25;
    // Верхняя граница множителя скорости. Используется: ChangeSpeed (Math.Clamp).
    private const double MaxSpeedFactor = 4.0;
    // [Д4] Минимальная начальная скорость точки (пикселей за тик). Используется: AddMovingGroup.
    private const double MinPointSpeed = 2.0;
    // [Д4] Максимальная начальная скорость точки. Используется: AddMovingGroup.
    private const double MaxPointSpeed = 5.0;

    // Область рисования (наш наследник Panel с двойной буферизацией). readonly — ссылка задаётся один раз в конструкторе.
    // Используется: конструктор (создание, подписки), везде _drawPanel.Invalidate() для перерисовки, MoveTimer_Tick (размеры).
    private readonly DrawingPanel _drawPanel;
    // Кнопка «Точки». Используется: конструктор (создание), UpdateButtonsVisual (подсветка зелёным, когда режим включён).
    private readonly Button _pointsButton;
    // Кнопка «Движение». Используется: конструктор (создание), UpdateButtonsVisual (подсветка, пока идёт движение).
    private readonly Button _moveButton;
    // Подсказка про горячие клавиши. Используется: только конструктор (создаётся и кладётся в панель; потом не читается).
    private readonly Label _hintLabel;
    // Таймер движения (именно WinForms Timer, поэтому полное имя — есть ещё System.Threading.Timer).
    // Используется: конструктор (Interval, Tick), ToggleMovement (Start), StopMovement (Stop).
    private readonly System.Windows.Forms.Timer _moveTimer;
    // Генератор случайных чисел. new() — целевой тип известен из объявления. Используется: AddMovingGroup (угол и скорость).
    private readonly Random _random = new();

    // Точки, которые ещё не зафиксированы кнопкой «Сохранить» — текущая, редактируемая фигура.
    // Список Point (struct). Используется: MouseDown (добавление точки), Paint, SaveCurrentShape, ClearAll, движение, стрелки, перетаскивание.
    private readonly List<Point> _currentPoints = new();

    // Фигуры, зафиксированные функцией Р1: они продолжают отображаться,
    // но новые точки по клику в их состав уже не добавляются.
    // [Р1] Список сохранённых фигур. Используется: SaveCurrentShape (Add), Paint, ClearAll, поиск точки, движение, стрелки.
    private readonly List<Shape> _savedShapes = new();

    // Текущий режим отображения (None/Curve/Polygon/Bezier/Filled). Используется: SetMode (пишет), Paint и SaveCurrentShape (читают).
    private ShapeMode _currentMode = ShapeMode.None;
    // Включено ли добавление точек кликом. Используется: ToggleAddPoints (кнопка «Точки»), DrawPanel_MouseDown, UpdateButtonsVisual.
    private bool _addPointsEnabled;
    // [П2] Цвет точек текущей фигуры (по умолчанию красный). Используется: OpenParameters (меняет), Paint, SaveCurrentShape.
    private Color _pointColor = Color.Red;
    // [П2] Цвет линии/кривой текущей фигуры (по умолчанию синий). Используется: OpenParameters (меняет), Paint, SaveCurrentShape.
    private Color _lineColor = Color.Blue;

    // [Д4] Идёт ли сейчас движение. Используется: ToggleMovement/StopMovement (пишут), MouseDown и ProcessCmdKey (блокируют действия), UpdateButtonsVisual.
    private bool _isMoving;
    // [Д4] Множитель скорости (1.0 = обычная). Скорость меняется ИМЕННО им, а не интервалом таймера.
    // Используется: ChangeSpeed (клавиши «+»/«-»), MoveTimer_Tick (умножение вектора).
    private double _speedFactor = 1.0;
    // [Д4] Список движущихся точек с их векторами. Используется: ToggleMovement, StopMovement, BuildMovingPoints, AddMovingGroup, MoveTimer_Tick.
    private List<MovingPoint> _movingPoints = new();

    // Перетаскивание точки мышью: список-владелец и индекс точки внутри него.
    // Список, которому принадлежит перетаскиваемая точка (_currentPoints или Points сохранённой фигуры); null = не тащим.
    // Используется: MouseDown (запоминает), MouseMove (проверяет и двигает), MouseUp (сбрасывает).
    private List<Point>? _draggedOwner;
    // Индекс перетаскиваемой точки в списке-владельце; -1 = нет. Используется: MouseDown / MouseMove / MouseUp.
    private int _draggedIndex = -1;

    // Конструктор формы: строит весь интерфейс в коде (дизайнера нет). Используется: вызывается из Program.Main через new MainForm().
    public MainForm()
    {
        // Форма: размер, положение при запуске, ограничения на изменение размера.
        Text = "Практика 3 — 2D-графика (Костин Д.А., вариант 3)"; // заголовок окна
        StartPosition = FormStartPosition.CenterScreen; // при запуске окно по центру экрана
        Size = new Size(1000, 700); // начальный размер окна 1000x700
        MinimumSize = new Size(760, 520); // меньше окно сжать нельзя
        MaximumSize = new Size(1600, 1000); // больше растянуть нельзя
        // KeyPreview = true: форма получает KeyDown ПЕРВОЙ, раньше, чем кнопка/панель с фокусом.
        // Без этого клавиши Space/+/-/Esc обрабатывались бы только тем элементом, на котором фокус. Используется: MainForm_KeyDown.
        KeyPreview = true;

        // Левая панель под кнопки: Dock.Left — прилипает к левому краю, ширина 170.
        var buttonsPanel = new Panel { Dock = DockStyle.Left, Width = 170, Padding = new Padding(10) };
        // Колонка кнопок: FlowLayoutPanel сам раскладывает кнопки друг под другом.
        var buttonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, // прижата к верху левой панели
            FlowDirection = FlowDirection.TopDown, // кнопки идут сверху вниз
            WrapContents = false, // не переносить в новый столбец
            AutoSize = true // высота по содержимому
        };

        // Создание кнопок. Второй аргумент — лямбда-обработчик Click (_, _) => метод: параметры sender/e не нужны.
        // Паттерн «подписка на событие + делегат EventHandler» — см. CreateButton.
        // «Точки» включает/выключает добавление точек кликом -> ToggleAddPoints.
        _pointsButton = CreateButton("Точки", (_, _) => ToggleAddPoints());
        // [П2] «Параметры» открывает окно с ComboBox цветов -> OpenParameters.
        var paramsButton = CreateButton("Параметры", (_, _) => OpenParameters());
        // «Кривая» -> режим замкнутой кривой (DrawClosedCurve).
        var curveButton = CreateButton("Кривая", (_, _) => SetMode(ShapeMode.Curve));
        // «Ломаная» -> режим многоугольника (DrawPolygon).
        var polygonButton = CreateButton("Ломаная", (_, _) => SetMode(ShapeMode.Polygon));
        // «Безье» -> режим кривых Безье (DrawBeziers, нужно (n-1)%3==0 точек).
        var bezierButton = CreateButton("Безье", (_, _) => SetMode(ShapeMode.Bezier));
        // «Заполненная» -> режим залитой кривой (FillClosedCurve).
        var filledButton = CreateButton("Заполненная", (_, _) => SetMode(ShapeMode.Filled));
        // [Д4] «Движение» включает/выключает движение точек (то же делает клавиша Space) -> ToggleMovement.
        _moveButton = CreateButton("Движение", (_, _) => ToggleMovement());
        // [Р1] «Сохранить» фиксирует текущую фигуру -> SaveCurrentShape.
        var saveButton = CreateButton("Сохранить", (_, _) => SaveCurrentShape());
        // «Очистить» удаляет все точки и фигуры (то же делает Esc) -> ClearAll.
        var clearButton = CreateButton("Очистить", (_, _) => ClearAll());

        // Кладём кнопки в колонку в том порядке, в котором они видны сверху вниз.
        buttonsFlow.Controls.AddRange(new Control[]
        {
            _pointsButton, paramsButton, curveButton, polygonButton, // первые четыре кнопки
            bezierButton, filledButton, _moveButton, saveButton, clearButton // остальные пять кнопок
        });

        // Серая подсказка по клавишам внизу левой панели (пользователь видит её слева снизу).
        _hintLabel = new Label
        {
            Dock = DockStyle.Bottom, // прижата к низу панели
            Height = 150, // высота под 5 строк текста
            // \n — перенос строки. Текст напоминает горячие клавиши (KeyDown) и стрелки (ProcessCmdKey).
            Text = "Space — движение\n+/- — скорость движения\nEsc — очистить\nСтрелки — сдвиг фигуры\n(недоступно в движении)",
            ForeColor = Color.DimGray // серый цвет текста
        };

        buttonsPanel.Controls.Add(buttonsFlow); // колонку кнопок кладём в левую панель
        buttonsPanel.Controls.Add(_hintLabel); // подсказку — тоже в левую панель

        // Область рисования: Dock.Fill занимает всё место правее кнопок, белый фон, тонкая рамка.
        _drawPanel = new DrawingPanel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        // Подписки на события панели через +=: при событии WinForms вызовет указанный метод (делегат-обработчик).
        // Paint — нужно перерисовать (после Invalidate) -> рисуем фигуры.
        _drawPanel.Paint += DrawPanel_Paint;
        // MouseDown — нажата кнопка мыши: добавить точку или начать перетаскивание.
        _drawPanel.MouseDown += DrawPanel_MouseDown;
        // MouseMove — мышь движется: если тащим точку, переставить её.
        _drawPanel.MouseMove += DrawPanel_MouseMove;
        // MouseUp — кнопка отпущена: закончить перетаскивание.
        _drawPanel.MouseUp += DrawPanel_MouseUp;

        // Панель с кнопками добавляется последней, чтобы Dock.Left не перекрывался Dock.Fill.
        Controls.Add(_drawPanel); // добавляем область рисования на форму
        Controls.Add(buttonsPanel); // добавляем левую панель с кнопками

        // [Д4] Таймер движения: срабатывает каждые 30 мс. Интервал НЕ меняется — скорость регулирует _speedFactor.
        _moveTimer = new System.Windows.Forms.Timer { Interval = 30 };
        // Каждый тик вызывает MoveTimer_Tick, но только пока таймер запущен (ToggleMovement -> Start).
        _moveTimer.Tick += MoveTimer_Tick;

        // Подписка на KeyDown формы (работает благодаря KeyPreview): Space, «+», «-», Esc.
        KeyDown += MainForm_KeyDown;

        // Расцветить кнопки «Точки»/«Движение» по начальному состоянию (обе выключены -> серые).
        UpdateButtonsVisual();
    } // конец конструктора MainForm

    // Фабрика кнопок, чтобы не повторять код 9 раз. EventHandler — тип-делегат обработчика события (object sender, EventArgs e).
    // Используется: только конструктор MainForm (создание девяти кнопок).
    private Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text, // надпись на кнопке
            Width = 140, // ширина
            Height = 36, // высота
            Margin = new Padding(0, 0, 0, 8) // отступ снизу между кнопками
        };
        // Подписываем переданный обработчик на клик: нажатие вызовет лямбду из конструктора.
        button.Click += onClick;
        return button; // возвращаем готовую кнопку в конструктор
    } // конец метода CreateButton

    // ------------------------------------------------------------------
    // Режим «Точки»: включает/выключает добавление новых точек по щелчку.
    // ------------------------------------------------------------------

    // Переключатель режима «Точки». Используется: кнопка «Точки» (лямбда в конструкторе).
    private void ToggleAddPoints()
    {
        // Инвертируем флаг: включено <-> выключено. Флаг читает DrawPanel_MouseDown.
        _addPointsEnabled = !_addPointsEnabled;
        UpdateButtonsVisual(); // кнопка «Точки» становится зелёной/серой
    } // конец метода ToggleAddPoints

    // Выбор способа отображения фигуры. Используется: кнопки «Кривая», «Ломаная», «Безье», «Заполненная».
    private void SetMode(ShapeMode mode)
    {
        _currentMode = mode; // запоминаем режим; его читает DrawPanel_Paint
        // Invalidate помечает панель «грязной»; WinForms сам позже вызовет Paint. Пользователь сразу видит новый вид фигуры.
        _drawPanel.Invalidate();
    } // конец метода SetMode

    // Подсветка кнопок. Используется: конструктор, ToggleAddPoints, ToggleMovement, StopMovement.
    private void UpdateButtonsVisual()
    {
        // Тернарный оператор: если режим включён — светло-зелёный, иначе обычный цвет кнопки.
        _pointsButton.BackColor = _addPointsEnabled ? Color.LightGreen : SystemColors.Control;
        // То же для «Движение»: зелёная, пока точки движутся.
        _moveButton.BackColor = _isMoving ? Color.LightGreen : SystemColors.Control;
    } // конец метода UpdateButtonsVisual

    // ------------------------------------------------------------------
    // Окно «Параметры» (П2): выбор цвета точек и цвета линии/кривой из ComboBox.
    // ------------------------------------------------------------------

    // [П2] Открывает дочернее окно. Используется: кнопка «Параметры».
    private void OpenParameters()
    {
        // using var — окно автоматически освободится (Dispose) при выходе из метода.
        // Передаём текущие цвета, чтобы ComboBox сразу показывали их.
        using var form = new ParametersForm(_pointColor, _lineColor);
        // ShowDialog — МОДАЛЬНОЕ окно: главное окно заблокировано, пока оно открыто, а код здесь ждёт закрытия.
        // (Show, наоборот, не блокирует и сразу возвращает управление.) this — родитель, окно откроется по центру него.
        // Результат — DialogResult: OK (нажали «ОК» или Enter) или Cancel.
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _pointColor = form.SelectedPointColor; // забираем выбранный цвет точек из свойства окна
            _lineColor = form.SelectedLineColor; // забираем выбранный цвет линии
            _drawPanel.Invalidate(); // перерисовать с новыми цветами
        }
    } // конец метода OpenParameters

    // ------------------------------------------------------------------
    // Сохранение фигуры (Р1).
    // ------------------------------------------------------------------

    // [Р1] Фиксирует текущую фигуру. Используется: кнопка «Сохранить».
    private void SaveCurrentShape()
    {
        if (_currentPoints.Count == 0) // нечего сохранять
        {
            return; // выходим, ничего не меняется
        }

        // [Р1] Shape делает КОПИЮ точек и запоминает режим и оба цвета на этот момент; добавляем в список сохранённых.
        _savedShapes.Add(new Shape(_currentPoints, _currentMode, _pointColor, _lineColor));
        // Очищаем текущие точки (копия в Shape не пострадает) — следующие клики начнут НОВУЮ фигуру.
        _currentPoints.Clear();
        _drawPanel.Invalidate(); // перерисовать: сохранённая фигура осталась на экране
    } // конец метода SaveCurrentShape

    // Полная очистка. Используется: кнопка «Очистить» и клавиша Esc (MainForm_KeyDown).
    private void ClearAll()
    {
        StopMovement(); // сначала остановить движение, иначе таймер будет двигать уже удалённые точки
        _currentPoints.Clear(); // стереть текущие точки
        _savedShapes.Clear(); // стереть сохранённые фигуры
        _drawPanel.Invalidate(); // перерисовать пустую панель
    } // конец метода ClearAll

    // ------------------------------------------------------------------
    // Отрисовка.
    // ------------------------------------------------------------------

    // Обработчик Paint: рисуем ЗДЕСЬ, а не «прямо в форму». Причина: Windows может в любой момент стереть окно
    // (свернули, перекрыли) и попросить перерисовать — тогда вызывается Paint, и всё рисуется заново из данных.
    // Рисунок, сделанный вне Paint, пропал бы. Используется: подписан в конструкторе (_drawPanel.Paint += ...); вызывается после Invalidate.
    private void DrawPanel_Paint(object? sender, PaintEventArgs e)
    {
        // e.Graphics — «холст» панели. AntiAlias сглаживает края линий и кругов.
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // [Р1] Сначала рисуем сохранённые фигуры (каждая со своим режимом и цветами, запомненными в Shape).
        foreach (Shape shape in _savedShapes)
        {
            DrawShape(e.Graphics, shape.Points, shape.Mode, shape.PointColor, shape.LineColor);
        }

        // Поверх них рисуем текущую (ещё не сохранённую) фигуру текущим режимом и цветами.
        DrawShape(e.Graphics, _currentPoints, _currentMode, _pointColor, _lineColor);
    } // конец метода DrawPanel_Paint

    // Рисует одну фигуру: сначала точки, потом линию по режиму. static — не использует поля формы.
    // Graphics — «холст» (набор методов рисования), Pen — перо для линий, Brush (SolidBrush) — кисть для заливки.
    // Используется: DrawPanel_Paint (для каждой сохранённой фигуры и для текущей).
    private static void DrawShape(Graphics g, List<Point> points, ShapeMode mode, Color pointColor, Color lineColor)
    {
        // using var — кисть освободится в конце метода (это неуправляемый ресурс GDI+, IDisposable).
        using var pointBrush = new SolidBrush(pointColor);
        foreach (Point p in points) // перебираем все точки фигуры
        {
            // Рисуем кружок: левый верхний угол = центр минус радиус, ширина и высота = диаметр. Так видны точки при любом режиме.
            g.FillEllipse(pointBrush, p.X - PointRadius, p.Y - PointRadius, PointRadius * 2, PointRadius * 2);
        }

        if (points.Count < 2) // для линии нужно хотя бы 2 точки
        {
            return; // рисуем только точки
        }

        // Перо толщиной 2 пикселя цветом линии; using var освободит его в конце метода.
        using var pen = new Pen(lineColor, 2);
        // switch по режиму. «case X when условие» — ветка сработает, только если режим совпал И условие истинно.
        switch (mode)
        {
            // Кривая: DrawClosedCurve строит ПЛАВНУЮ замкнутую кривую (сплайн), проходящую через все точки; нужно ≥3 точек.
            // Ему передаётся массив Point[] — поэтому List превращаем через ToArray().
            case ShapeMode.Curve when points.Count >= 3:
                g.DrawClosedCurve(pen, points.ToArray()); // рисуем кривую по массиву точек
                break;

            // Ломаная: DrawPolygon соединяет точки прямыми отрезками и замыкает последнюю с первой.
            case ShapeMode.Polygon when points.Count >= 3:
                g.DrawPolygon(pen, points.ToArray()); // рисуем многоугольник
                break;

            // Безье: DrawBeziers берёт точки группами: начало, 2 управляющие, конец; следующая кривая начинается с конца предыдущей.
            case ShapeMode.Bezier:
                // Берём подходящее число точек (4, 7, 10...), лишние игнорируются — см. GetBezierSubset. null = точек мало.
                Point[]? bezierPoints = GetBezierSubset(points);
                if (bezierPoints is not null) // проверка на null: если точек < 4, ничего не рисуем
                {
                    g.DrawBeziers(pen, bezierPoints); // рисуем кривые Безье
                }
                break;

            // Заполненная: FillClosedCurve закрашивает область той же плавной кривой, потом сверху рисуем контур.
            case ShapeMode.Filled when points.Count >= 3:
                // Кисть заливки: цвет линии с прозрачностью 160 из 255 (полупрозрачная), чтобы контур был виден.
                using (var fillBrush = new SolidBrush(Color.FromArgb(160, lineColor)))
                {
                    g.FillClosedCurve(fillBrush, points.ToArray()); // заливаем внутренность кривой
                } // кисть освобождается здесь
                g.DrawClosedCurve(pen, points.ToArray()); // контур поверх заливки
                break;
        }
    } // конец метода DrawShape

    /// <summary>
    /// DrawBeziers требует ровно 4, 7, 10... точек, т.е. (n - 1) кратно 3.
    /// Если точек больше, но их число "неровное", используем наибольшее подходящее
    /// подмножество, начиная с первой точки, — так все лишние точки для Безье
    /// просто не участвуют в построении кривой, вместо того чтобы требовать от
    /// пользователя точного количества точек.
    /// </summary>
    // Используется: DrawShape, ветка ShapeMode.Bezier. Возвращает массив (для DrawBeziers) или null; «?» = может быть null (Nullable).
    private static Point[]? GetBezierSubset(List<Point> points)
    {
        if (points.Count < 4) // меньше 4 точек — Безье построить нельзя
        {
            return null; // сигнал «нечего рисовать»
        }

        // Целочисленное деление: наибольшее число вида 3k+1 не больше n. Пример: n=6 -> (5/3)*3+1 = 4; n=7 -> 7.
        int usableCount = ((points.Count - 1) / 3) * 3 + 1;
        // Берём первые usableCount точек и превращаем в массив: именно массив Point[] принимает DrawBeziers.
        return points.GetRange(0, usableCount).ToArray();
    } // конец метода GetBezierSubset

    // ------------------------------------------------------------------
    // Мышь: добавление точек кликом и перетаскивание существующих точек.
    // ------------------------------------------------------------------

    // Нажатие кнопки мыши на панели. Используется: подписан в конструкторе (_drawPanel.MouseDown). e.Location — где кликнули.
    private void DrawPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_isMoving) // во время движения точки не трогаем: ими управляет таймер
        {
            return; // ни добавления, ни перетаскивания
        }

        // Ищем точку под курсором. out-параметры: метод возвращает bool И «отдаёт» через owner/index ещё два значения.
        if (TryFindPointAt(e.Location, out List<Point>? owner, out int index))
        {
            _draggedOwner = owner; // запоминаем список, где лежит точка — с этого момента идёт перетаскивание
            _draggedIndex = index; // и её номер в списке
            return; // новую точку в этом случае не ставим
        }

        if (_addPointsEnabled) // под курсором пусто; если режим «Точки» включён
        {
            _currentPoints.Add(e.Location); // добавляем новую точку в текущую фигуру
            _drawPanel.Invalidate(); // перерисовать, пользователь видит новую точку
        }
    } // конец метода DrawPanel_MouseDown

    // Движение мыши по панели. Используется: подписан в конструкторе (_drawPanel.MouseMove); срабатывает постоянно, пока курсор над панелью.
    private void DrawPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        // Если перетаскивание не начато (не нажали на точку) — ничего не делаем.
        if (_draggedOwner is null || _draggedIndex < 0)
        {
            return; // выход
        }

        // List<Point> хранит СТРУКТУРЫ (значения), поэтому нельзя написать list[i].X = ...:
        // индексатор вернул бы копию. Приходится ЗАМЕНЯТЬ элемент целиком новым Point.
        _draggedOwner[_draggedIndex] = e.Location;
        _drawPanel.Invalidate(); // перерисовать: точка «едет» за мышью, фигура перестраивается
    } // конец метода DrawPanel_MouseMove

    // Отпускание кнопки мыши. Используется: подписан в конструкторе (_drawPanel.MouseUp).
    private void DrawPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        _draggedOwner = null; // перетаскивание закончено: список больше не «захвачен»
        _draggedIndex = -1; // индекса нет
    } // конец метода DrawPanel_MouseUp

    /// <summary>
    /// Ищет точку рядом с указанными координатами среди текущей и всех сохранённых фигур.
    /// Сначала проверяется текущая фигура и более поздние сохранённые — так перетаскивается
    /// та точка, что визуально находится "сверху".
    /// </summary>
    // Используется: DrawPanel_MouseDown. Возвращает true, если нашли; owner и index — «выходные» значения (out).
    private bool TryFindPointAt(Point location, out List<Point>? owner, out int index)
    {
        // Сначала ищем среди текущих точек (они нарисованы поверх всех).
        if (TryFindPointIn(_currentPoints, location, out index))
        {
            owner = _currentPoints; // отдаём наружу, чей это список
            return true; // нашли
        }

        // Затем сохранённые фигуры с последней к первой (последняя нарисована выше).
        for (int s = _savedShapes.Count - 1; s >= 0; s--)
        {
            if (TryFindPointIn(_savedShapes[s].Points, location, out index)) // ищем в точках s-й фигуры
            {
                owner = _savedShapes[s].Points; // список этой фигуры — именно его будем менять при перетаскивании
                return true; // нашли
            }
        }

        // Ничего не нашли: out-параметры обязательно нужно заполнить до return.
        owner = null;
        index = -1;
        return false; // клик пришёлся на пустое место
    } // конец метода TryFindPointAt

    // Ищет в одном списке точку в радиусе PointHitRadius. Используется: TryFindPointAt (для текущей и сохранённых фигур).
    private static bool TryFindPointIn(List<Point> points, Point location, out int index)
    {
        // Идём с конца: позже добавленная точка нарисована выше, её и «ловим» первой.
        for (int i = points.Count - 1; i >= 0; i--)
        {
            int dx = points[i].X - location.X; // разница по X между точкой и кликом
            int dy = points[i].Y - location.Y; // разница по Y
            // Теорема Пифагора без корня: расстояние² <= радиус². Если да — клик попал по точке.
            if (dx * dx + dy * dy <= PointHitRadius * PointHitRadius)
            {
                index = i; // отдаём номер найденной точки
                return true; // нашли
            }
        }

        index = -1; // не нашли: индекса нет
        return false; // возвращаем «не найдено»
    } // конец метода TryFindPointIn

    // ------------------------------------------------------------------
    // Движение (Д4).
    // ------------------------------------------------------------------

    /// <summary>
    /// Точка в режиме движения: ссылка на список-владелец (текущая или одна из
    /// сохранённых фигур) и индекс внутри него, плюс собственный вектор скорости,
    /// выбранный один раз случайным образом в момент старта движения.
    /// </summary>
    // [Д4] Вложенный приватный класс: одна запись на каждую движущуюся точку. Используется: список _movingPoints,
    // создаётся в AddMovingGroup, читается и меняется в MoveTimer_Tick.
    private sealed class MovingPoint
    {
        // required — свойство ОБЯЗАТЕЛЬНО указать при создании (new MovingPoint { Owner = ... }), иначе ошибка компиляции.
        // init — значение задаётся только при создании и потом не меняется. Owner — какому списку принадлежит точка.
        // Используется: пишется в AddMovingGroup, читается в MoveTimer_Tick (mp.Owner[mp.Index]).
        public required List<Point> Owner { get; init; }
        // Индекс точки в списке Owner (required + init, как выше). Используется: AddMovingGroup / MoveTimer_Tick.
        public required int Index { get; init; }
        // [Д4] Скорость по X (пикселей за тик). set — меняется: у ВСЕХ точек знак инвертируется при ударе. Используется: AddMovingGroup, MoveTimer_Tick.
        public double Vx { get; set; }
        // [Д4] Скорость по Y; меняется так же. Используется: AddMovingGroup, MoveTimer_Tick.
        public double Vy { get; set; }
    } // конец класса MovingPoint

    // [Д4] Включает/выключает движение. Используется: кнопка «Движение» и клавиша Space (MainForm_KeyDown).
    private void ToggleMovement()
    {
        if (_isMoving) // уже движется — значит, нажали повторно, чтобы остановить
        {
            StopMovement(); // останавливаем
            return; // выход
        }

        BuildMovingPoints(); // собираем все точки и выдаём каждой случайный вектор
        if (_movingPoints.Count == 0) // точек нет — двигать нечего
        {
            return; // остаёмся в покое
        }

        _isMoving = true; // включаем флаг движения (он блокирует мышь и стрелки С1)
        _moveTimer.Start(); // запускаем таймер: пойдут вызовы MoveTimer_Tick каждые 30 мс
        UpdateButtonsVisual(); // кнопка «Движение» становится зелёной
    } // конец метода ToggleMovement

    // Останавливает движение. Используется: ToggleMovement (повторное нажатие), ClearAll, MoveTimer_Tick (если точек не осталось).
    private void StopMovement()
    {
        _moveTimer.Stop(); // тики прекращаются
        _isMoving = false; // разблокируем мышь и стрелки
        _movingPoints.Clear(); // забываем векторы: при новом старте выдадутся новые
        UpdateButtonsVisual(); // кнопка «Движение» снова серая
    } // конец метода StopMovement

    /// <summary>
    /// Собирает все отображаемые точки (текущую фигуру и все сохранённые) в единый
    /// список для движения. Направление и скорость каждой точки выбираются случайно
    /// один раз здесь и больше не меняются сами по себе — меняется только знак
    /// составляющих при столкновении (см. MoveTimer_Tick).
    /// </summary>
    // [Д4] Используется: ToggleMovement (при старте движения).
    private void BuildMovingPoints()
    {
        _movingPoints = new List<MovingPoint>(); // новый пустой список движущихся точек

        AddMovingGroup(_currentPoints); // добавляем точки текущей фигуры
        foreach (Shape shape in _savedShapes) // и всех сохранённых (Р1 — они тоже двигаются)
        {
            AddMovingGroup(shape.Points); // точки одной сохранённой фигуры
        }
    } // конец метода BuildMovingPoints

    // [Д4] Выдаёт каждой точке группы свой случайный вектор скорости. Используется: BuildMovingPoints.
    private void AddMovingGroup(List<Point> group)
    {
        for (int i = 0; i < group.Count; i++) // по каждой точке списка
        {
            // Случайный угол в радианах от 0 до 2π — направление движения.
            double angle = _random.NextDouble() * Math.PI * 2;
            // Случайная скорость от MinPointSpeed до MaxPointSpeed (2..5 пикселей за тик).
            double speed = MinPointSpeed + _random.NextDouble() * (MaxPointSpeed - MinPointSpeed);

            _movingPoints.Add(new MovingPoint
            {
                Owner = group, // запоминаем, в каком списке лежит точка
                Index = i, // и её номер
                Vx = Math.Cos(angle) * speed, // проекция скорости на X (у КАЖДОЙ точки своя)
                Vy = Math.Sin(angle) * speed // проекция скорости на Y
            });
        }
    } // конец метода AddMovingGroup

    // [Д4] Один шаг анимации. Используется: подписан на _moveTimer.Tick в конструкторе; вызывается каждые 30 мс, пока движение включено.
    private void MoveTimer_Tick(object? sender, EventArgs e)
    {
        if (_movingPoints.Count == 0) // страховка: двигаться нечему
        {
            StopMovement(); // выключаем таймер
            return; // выход
        }

        int width = _drawPanel.ClientSize.Width; // текущая ширина области рисования (границы окна)
        int height = _drawPanel.ClientSize.Height; // текущая высота

        // Сначала проверяем, не выйдет ли при текущем направлении хоть одна точка
        // за границы окна. Если да — у ВСЕХ точек инвертируется знак соответствующей
        // составляющей скорости (Д4: столкновение одной точки разворачивает все точки).
        bool flipX = false; // флаг: надо развернуть всех по X
        bool flipY = false; // флаг: надо развернуть всех по Y

        // [Д4] ПРОХОД 1: только проверка, точки пока не двигаем.
        foreach (MovingPoint mp in _movingPoints)
        {
            Point current = mp.Owner[mp.Index]; // текущее положение точки (Point — структура, это копия)
            double nextX = current.X + mp.Vx * _speedFactor; // где точка окажется по X на следующем шаге
            double nextY = current.Y + mp.Vy * _speedFactor; // и по Y; _speedFactor меняют «+»/«-»

            // Если следующее положение вышло за левый или правый край (с отступом на радиус точки) — ставим флаг.
            if (nextX <= PointRadius || nextX >= width - PointRadius)
            {
                flipX = true; // ЛЮБАЯ одна точка ударилась -> флаг для всех
            }
            // То же для верхнего и нижнего края.
            if (nextY <= PointRadius || nextY >= height - PointRadius)
            {
                flipY = true; // любая одна точка ударилась -> флаг для всех
            }
        }

        // [Д4] ПРОХОД 2: применяем развороты ко ВСЕМ точкам сразу — это и есть особенность варианта Д4.
        if (flipX)
        {
            foreach (MovingPoint mp in _movingPoints)
            {
                mp.Vx = -mp.Vx; // меняем знак X-составляющей у каждой точки
            }
        }
        if (flipY)
        {
            foreach (MovingPoint mp in _movingPoints)
            {
                mp.Vy = -mp.Vy; // меняем знак Y-составляющей у каждой точки
            }
        }

        // [Д4] ПРОХОД 3: сдвигаем каждую точку на вектор * множитель скорости.
        // Скорость меняется именно величиной шага, а не интервалом таймера.
        foreach (MovingPoint mp in _movingPoints)
        {
            Point current = mp.Owner[mp.Index]; // читаем текущее положение
            // Новая X: округляем до целого и зажимаем в границы, чтобы точка не вылетела из панели.
            int newX = Clamp((int)Math.Round(current.X + mp.Vx * _speedFactor), PointRadius, width - PointRadius);
            // Новая Y — так же.
            int newY = Clamp((int)Math.Round(current.Y + mp.Vy * _speedFactor), PointRadius, height - PointRadius);
            // Point — структура, поэтому меняем не X по месту, а кладём в список новый Point. Пользователь видит сдвиг после Invalidate.
            mp.Owner[mp.Index] = new Point(newX, newY);
        }

        _drawPanel.Invalidate(); // перерисовать кадр с новыми положениями (вызовет Paint)
    } // конец метода MoveTimer_Tick

    // Ограничивает число диапазоном [min, max]. Выражение-тело (=>). Используется: MoveTimer_Tick (координаты X и Y).
    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

    // [Д4] Меняет множитель скорости. Используется: MainForm_KeyDown (клавиши «+» и «-»).
    private void ChangeSpeed(double delta)
    {
        // Math.Clamp не даёт выйти за 0.25..4.0. Интервал таймера не трогаем — скорость влияет через _speedFactor в MoveTimer_Tick.
        _speedFactor = Math.Clamp(_speedFactor + delta, MinSpeedFactor, MaxSpeedFactor);
    } // конец метода ChangeSpeed

    // ------------------------------------------------------------------
    // Клавиатура.
    // ------------------------------------------------------------------

    // Обработчик нажатия клавиш формы. Работает благодаря KeyPreview = true. Используется: подписан в конструкторе (KeyDown += ...).
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode) // e.KeyCode — какая клавиша нажата
        {
            case Keys.Space: // пробел = кнопка «Движение»
                ToggleMovement(); // [Д4] включить/выключить движение
                e.SuppressKeyPress = true; // иначе пробел ещё и "нажмёт" кнопку, находящуюся в фокусе
                e.Handled = true; // сообщаем: клавиша обработана, дальше не передавать
                break;

            case Keys.Oemplus: // «+» на основной клавиатуре
            case Keys.Add: // «+» на цифровой клавиатуре (два case подряд — одно действие)
                ChangeSpeed(SpeedStep); // ускорить (+0.25 к множителю)
                e.Handled = true; // клавиша обработана
                break;

            case Keys.OemMinus: // «-» на основной клавиатуре
            case Keys.Subtract: // «-» на цифровой клавиатуре
                ChangeSpeed(-SpeedStep); // замедлить (-0.25)
                e.Handled = true; // клавиша обработана
                break;

            case Keys.Escape: // Esc = кнопка «Очистить»
                ClearAll(); // стереть всё
                e.Handled = true; // клавиша обработана
                break;
        }
    } // конец метода MainForm_KeyDown

    /// <summary>
    /// Стрелки перехватываются здесь, а не в KeyDown, потому что WinForms обрабатывает
    /// их как клавиши навигации фокуса ещё до того, как событие KeyDown дойдёт до формы
    /// (особенно когда фокус находится на одной из кнопок). Функция С1: сдвигают всю
    /// фигуру, но неприменимы в режиме движения.
    /// </summary>
    // [С1] override — переопределяем метод базового класса Form. ProcessCmdKey вызывается на самом раннем этапе обработки клавиши,
    // ДО KeyDown и ДО того, как стрелка переключит фокус между кнопками. Возврат true = «клавиша съедена, дальше не передавать».
    // Используется: вызывается самой WinForms при каждом нажатии клавиши (вручную нигде не вызывается).
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_isMoving) // [С1] в режиме движения стрелки не работают: блок пропускается
        {
            switch (keyData) // keyData — код нажатой клавиши (с модификаторами)
            {
                case Keys.Left: // стрелка влево
                    ShiftEverything(-ArrowShiftStep, 0); // сдвиг влево на 8 пикселей
                    return true; // клавиша обработана, фокус не переключается
                case Keys.Right: // стрелка вправо
                    ShiftEverything(ArrowShiftStep, 0); // сдвиг вправо
                    return true; // обработано
                case Keys.Up: // стрелка вверх
                    ShiftEverything(0, -ArrowShiftStep); // вверх (ось Y направлена вниз, поэтому минус)
                    return true; // обработано
                case Keys.Down: // стрелка вниз
                    ShiftEverything(0, ArrowShiftStep); // вниз
                    return true; // обработано
            }
        }

        // Все остальные клавиши (и стрелки при движении) — обычная обработка по умолчанию.
        return base.ProcessCmdKey(ref msg, keyData);
    } // конец метода ProcessCmdKey

    // [С1] Сдвигает ВСЕ фигуры на экране (текущую и сохранённые). Используется: ProcessCmdKey (четыре стрелки).
    private void ShiftEverything(int shiftX, int shiftY)
    {
        ShiftPoints(_currentPoints, shiftX, shiftY); // сдвигаем текущие точки
        foreach (Shape shape in _savedShapes) // затем каждую сохранённую фигуру
        {
            ShiftPoints(shape.Points, shiftX, shiftY); // сдвиг точек фигуры
        }

        _drawPanel.Invalidate(); // перерисовать: пользователь видит фигуру на новом месте
    } // конец метода ShiftEverything

    // [С1] Сдвигает все точки одного списка. Используется: ShiftEverything.
    private static void ShiftPoints(List<Point> points, int shiftX, int shiftY)
    {
        for (int i = 0; i < points.Count; i++) // по всем точкам списка
        {
            // Point — структура: изменить X «по месту» нельзя, поэтому создаём новый Point и записываем его на то же место.
            points[i] = new Point(points[i].X + shiftX, points[i].Y + shiftY);
        }
    } // конец метода ShiftPoints
} // конец класса MainForm
