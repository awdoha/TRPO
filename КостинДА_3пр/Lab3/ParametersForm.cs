// Пространство имён проекта. Используется: общее для всех файлов Lab3.
namespace Lab3;

/// <summary>
/// Дочернее модальное окно «Параметры» (функция П2 по варианту).
/// Цвет точек и цвет кривой/линии выбираются из выпадающего списка
/// с предустановленными именами цветов, без ColorDialog.
/// </summary>
// [П2] Класс окна параметров. Используется: MainForm.OpenParameters создаёт его (кнопка «Параметры») и показывает через ShowDialog.
public sealed class ParametersForm : Form
{
    // Небольшой, но достаточно разнообразный набор предустановленных цветов —
    // полный список System.Drawing.KnownColor избыточен и содержит служебные системные цвета.
    // [П2] Имена цветов для ComboBox. static readonly — один общий неизменяемый массив на весь класс.
    // Используется: CreateColorCombo (наполняет Items обоих списков).
    private static readonly string[] PresetColorNames =
    {
        "Black", "White", "Gray", "Red", "Orange", "Gold", "Yellow", // первая группа цветов списка
        "Green", "DarkGreen", "Teal", "Blue", "Navy", "LightBlue", // вторая группа
        "Purple", "Magenta", "Pink", "Brown", "Maroon" // третья группа
    };

    // ComboBox выбора цвета точек. Используется: конструктор (создаётся, событие), OkButton_Click (читается .Text).
    private readonly ComboBox _pointColorCombo;
    // ComboBox выбора цвета линии. Используется: конструктор (создаётся, событие), OkButton_Click (читается .Text).
    private readonly ComboBox _lineColorCombo;
    // Квадратик-превью цвета точек. Используется: конструктор (создаётся и обновляется при смене выбора).
    private readonly Panel _pointColorPreview;
    // Квадратик-превью цвета линии. Используется: конструктор (создаётся и обновляется при смене выбора).
    private readonly Panel _lineColorPreview;

    // Результат для главной формы. private set — снаружи можно только читать.
    // Используется: пишется в конструкторе и OkButton_Click; читается в MainForm.OpenParameters после DialogResult.OK.
    public Color SelectedPointColor { get; private set; }
    // Выбранный цвет линии. Используется: так же, как SelectedPointColor (MainForm.OpenParameters -> _lineColor).
    public Color SelectedLineColor { get; private set; }

    // Конструктор: принимает текущие цвета, чтобы окно открылось с ними.
    // Используется: MainForm.OpenParameters -> new ParametersForm(_pointColor, _lineColor).
    public ParametersForm(Color currentPointColor, Color currentLineColor)
    {
        // Пока пользователь ничего не выбрал, результат = текущие цвета (нажатие «Отмена» их не меняет).
        SelectedPointColor = currentPointColor;
        SelectedLineColor = currentLineColor; // то же для цвета линии

        Text = "Параметры"; // заголовок окна
        // FixedDialog — окно нельзя растягивать, стиль диалога.
        FormBorderStyle = FormBorderStyle.FixedDialog;
        // Открыть по центру родительского окна (родитель передаётся в ShowDialog(this)).
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; // убрать кнопку «развернуть»
        MinimizeBox = false; // убрать кнопку «свернуть»
        ClientSize = new Size(300, 160); // размер рабочей области окна

        // Надпись слева от списка. Пользователь видит «Цвет точек:».
        var pointLabel = new Label { Text = "Цвет точек:", Location = new Point(15, 20), AutoSize = true };
        // [П2] Создаём выпадающий список цветов точек, выбран текущий цвет.
        _pointColorCombo = CreateColorCombo(new Point(120, 16), currentPointColor);
        // Квадратик справа от списка, показывает выбранный цвет.
        _pointColorPreview = CreatePreviewPanel(new Point(260, 18), currentPointColor);
        // Лямбда-обработчик события SelectedIndexChanged: срабатывает при выборе пункта в ComboBox.
        // (_, _) — «отбрасываемые» параметры sender и e, они не нужны. Color.FromName превращает текст ("Red") в цвет.
        _pointColorCombo.SelectedIndexChanged += (_, _) =>
            _pointColorPreview.BackColor = Color.FromName(_pointColorCombo.Text); // перекрашиваем превью

        // Надпись для второго списка.
        var lineLabel = new Label { Text = "Цвет кривой/линии:", Location = new Point(15, 60), AutoSize = true };
        // [П2] Второй ComboBox — цвет линии/кривой.
        _lineColorCombo = CreateColorCombo(new Point(120, 56), currentLineColor);
        // Превью цвета линии.
        _lineColorPreview = CreatePreviewPanel(new Point(260, 58), currentLineColor);
        // Обработчик смены выбора для второго списка — так же обновляет превью.
        _lineColorCombo.SelectedIndexChanged += (_, _) =>
            _lineColorPreview.BackColor = Color.FromName(_lineColorCombo.Text); // перекрашиваем превью

        // Кнопка ОК: DialogResult.OK при нажатии сама закроет окно и вернёт OK из ShowDialog в MainForm.OpenParameters.
        var okButton = new Button { Text = "ОК", Location = new Point(110, 110), Size = new Size(80, 30), DialogResult = DialogResult.OK };
        // Кнопка «Отмена»: DialogResult.Cancel закрывает окно, MainForm цвета не меняет.
        var cancelButton = new Button { Text = "Отмена", Location = new Point(200, 110), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };
        // Подписка обработчика на Click кнопки ОК: перед закрытием запишет выбранные цвета в свойства.
        okButton.Click += OkButton_Click;

        // Добавляем все элементы на форму, иначе они не появятся.
        Controls.AddRange(new Control[]
        {
            pointLabel, _pointColorCombo, _pointColorPreview, // элементы первой строки
            lineLabel, _lineColorCombo, _lineColorPreview, // элементы второй строки
            okButton, cancelButton // кнопки
        });

        AcceptButton = okButton; // Enter нажимает «ОК»
        CancelButton = cancelButton; // Esc нажимает «Отмена»
    } // конец конструктора ParametersForm

    // [П2] Фабрика ComboBox. Используется: дважды в конструкторе (для цвета точек и цвета линии).
    private static ComboBox CreateColorCombo(Point location, Color initialColor)
    {
        var combo = new ComboBox
        {
            Location = location, // куда поставить на форме
            Size = new Size(130, 24), // размер
            // DropDownList — можно только выбрать пункт, вписать свой текст нельзя.
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        combo.Items.AddRange(PresetColorNames); // заполняем список именами цветов

        // Все цвета в приложении назначаются только через этот же предустановленный список,
        // поэтому текущий цвет всегда должен в нём находиться.
        // Ищем текущий цвет по имени (Color.Red.Name == "Red"); -1 если не найден.
        int index = combo.Items.IndexOf(initialColor.Name);
        // Показываем найденный пункт, а если его нет — первый ("Black").
        combo.SelectedIndex = index >= 0 ? index : 0;

        return combo; // готовый список уходит в поле _pointColorCombo / _lineColorCombo
    } // конец метода CreateColorCombo

    // Фабрика квадратика-превью. Выражение-тело (=>) с целевым типом new() — создаёт Panel. Используется: дважды в конструкторе.
    private static Panel CreatePreviewPanel(Point location, Color color) => new()
    {
        Location = location, // позиция на форме
        Size = new Size(24, 24), // размер квадратика
        BackColor = color, // заливка = цвет
        BorderStyle = BorderStyle.FixedSingle // тонкая рамка
    };

    // [П2] Обработчик клика по «ОК». Используется: подписан в конструкторе (okButton.Click += ...).
    // Срабатывает ДО закрытия окна; затем DialogResult.OK кнопки закрывает окно.
    private void OkButton_Click(object? sender, EventArgs e)
    {
        // Переводим текст выбранного пункта в Color и сохраняем в свойство — его прочитает MainForm.OpenParameters.
        SelectedPointColor = Color.FromName(_pointColorCombo.Text);
        SelectedLineColor = Color.FromName(_lineColorCombo.Text); // то же для цвета линии
    } // конец метода OkButton_Click
} // конец класса ParametersForm
