using System.IO; // Пространство имён для IOException. Используется: в SafeClear (catch IOException). Примечание: при ImplicitUsings=enable оно подключено и так, строка дублирует.
using System.Reflection; // Классы рефлексии: Assembly, MemberInfo, MethodInfo, PropertyInfo, FieldInfo, ReflectionTypeLoadException. Используются в ShowTypeInfo, ShowAllTypeInfo, GetAllTypesFromLoadedAssemblies.
using System.Text; // Нужен ради Encoding.UTF8. Используется: в Main (Console.OutputEncoding = Encoding.UTF8).
using System.Linq; // LINQ-методы: Select, Where, GroupBy, OrderBy, Distinct, Count, Take, ToArray, ToList. Используются почти во всех методах ниже (тоже подключён неявно через ImplicitUsings).

namespace Lab1; // Файловое пространство имён: всё содержимое файла (Program, DemoRecord) лежит в Lab1; TypeUtils из TypeUtils.cs — в том же пространстве, поэтому вызывается без using.

// Статический класс-контейнер для консольного приложения (экземпляры создавать нельзя). Используется: содержит точку входа Main, с которой .NET начинает выполнение при запуске.
public static class Program
{
    // Номер моего варианта. Используется: в ShowMethodsByVariant (строка заголовка «Вариант ...») и в ShowAllTypeInfo (строка «V = ...»), только для вывода на экран. const = значение зашито при компиляции.
    private const int Variant = 4;

    // Соответствие пункта меню и типа, который анализируется через рефлексию.
    // Словарь: ключ - символ клавиши ('1'..'9'), значение - объект Type. Используется: в SelectType (TryGetValue), чтобы по нажатой клавише получить Type без длинного switch.
    // readonly - ссылку на словарь нельзя заменить; static - словарь один на весь класс; new() - короткая запись создания (тип выводится из объявления).
    private static readonly Dictionary<char, Type> TypeMenu = new()
    {
        ['1'] = typeof(uint), // typeof(...) - получить Type по имени типа, известному при компиляции. Пункт 1 меню «выбор типа» -> uint (System.UInt32).
        ['2'] = typeof(int), // Пункт 2 -> int (System.Int32), значимый тип.
        ['3'] = typeof(long), // Пункт 3 -> long (System.Int64), значимый тип.
        ['4'] = typeof(float), // Пункт 4 -> float (System.Single), значимый тип.
        ['5'] = typeof(double), // Пункт 5 -> double (System.Double), значимый тип.
        ['6'] = typeof(char), // Пункт 6 -> char (System.Char), значимый тип.
        ['7'] = typeof(string), // Пункт 7 -> string (System.String), ссылочный тип.
        ['8'] = typeof(DemoRecord), // Пункт 8 -> мой record DemoRecord (объявлен внизу файла), чтобы показать члены, сгенерированные компилятором.
        ['9'] = typeof(Tuple<int, string>) // Пункт 9 -> закрытый generic-тип Tuple<int,string> (поля-свойства Item1, Item2).
    };

    // Точка входа: .NET вызывает Main при старте программы (dotnet run). Используется: запускается автоматически, из кода никем не вызывается. Из неё идут вызовы всех остальных методов.
    public static void Main()
    {
        // Включаем вывод в UTF-8, чтобы русские буквы в консоли не превращались в «кракозябры». Действует на все последующие Console.WriteLine.
        Console.OutputEncoding = Encoding.UTF8;

        // Бесконечный цикл главного меню: после каждого действия меню рисуется заново; выход - только через return в case '0'.
        while (true)
        {
            ShowMainMenu(); // Очищает экран и печатает пункты главного меню (см. ShowMainMenu).
            // Главное меню работает на чтении одной нажатой клавиши.
            // switch по символу, возвращённому ReadMenuKey(): выбирается ветка, соответствующая нажатой клавише. Клавиша, для которой нет case, просто игнорируется - цикл идёт на новый круг.
            switch (ReadMenuKey())
            {
                case '1': // Клавиша 1 в главном меню.
                    ShowAllTypeInfo(); // Экран «Общая информация по типам» (сводка по всем загруженным типам + результаты варианта 4).
                    break; // Выходим из switch, цикл while показывает меню снова.
                case '2': // Клавиша 2.
                    SelectType(); // Экран выбора одного из 9 типов и просмотр подробностей.
                    break; // Возврат в меню после выхода из подменю.
                case '3': // Клавиша 3.
                    ChangeConsoleView(); // Экран настройки цвета шрифта и фона консоли.
                    break; // Возврат в главное меню.
                case '0': // Клавиша 0 - выход.
                    return; // Завершаем Main, а значит и всю программу.
            }
        } // конец цикла while(true) главного меню
    } // конец метода Main

    // Рисует главное меню. Используется: вызывается в начале каждого витка цикла в Main.
    private static void ShowMainMenu()
    {
        // Метод Clear вызывается перед каждым экраном, чтобы не смешивать вывод.
        SafeClear(); // Безопасная очистка экрана (не падает, если консоль перенаправлена в файл/пайп).
        Console.WriteLine("Информация по типам:"); // Заголовок главного меню.
        Console.WriteLine("1 - Общая информация по типам"); // Подсказка: клавиша 1 -> ShowAllTypeInfo (case '1' в Main).
        Console.WriteLine("2 - Выбрать тип из списка"); // Подсказка: клавиша 2 -> SelectType.
        Console.WriteLine("3 - Параметры консоли"); // Подсказка: клавиша 3 -> ChangeConsoleView.
        Console.WriteLine("0 - Выход из программы"); // Подсказка: клавиша 0 -> return из Main.
    } // конец метода ShowMainMenu

    // Экран выбора типа (пункт 2 главного меню). Используется: вызывается из Main (case '2'). Вызывает ShowTypeInfo для выбранного типа.
    private static void SelectType()
    {
        // Цикл нужен, чтобы при нажатии неверной клавиши меню показывалось снова.
        while (true)
        {
            // Экран выбора базового типа из задания.
            SafeClear(); // Очищаем экран перед показом списка.
            Console.WriteLine("Информация по типам"); // Заголовок экрана.
            Console.WriteLine("Выберите тип:"); // Приглашение к выбору.
            Console.WriteLine("----------------------------------------"); // Визуальный разделитель.
            Console.WriteLine("1 - uint"); // Пункт меню; соответствует ключу '1' в словаре TypeMenu.
            Console.WriteLine("2 - int"); // Ключ '2' в TypeMenu.
            Console.WriteLine("3 - long"); // Ключ '3' в TypeMenu.
            Console.WriteLine("4 - float"); // Ключ '4' в TypeMenu.
            Console.WriteLine("5 - double"); // Ключ '5' в TypeMenu.
            Console.WriteLine("6 - char"); // Ключ '6' в TypeMenu.
            Console.WriteLine("7 - string"); // Ключ '7' в TypeMenu.
            Console.WriteLine("8 - record"); // Ключ '8' в TypeMenu (DemoRecord).
            Console.WriteLine("9 - Tuple<int, string>"); // Ключ '9' в TypeMenu.
            Console.WriteLine("0 - Выход в главное меню"); // Клавиша 0 обрабатывается ниже (return).

            char key = ReadMenuKey(); // Ждём нажатия клавиши и запоминаем символ. Используется: дальше в if (key == '0') и в TypeMenu.TryGetValue.
            if (key == '0') // Пользователь хочет назад.
            {
                return; // Выходим из SelectType -> управление возвращается в Main, показывается главное меню.
            }

            // Если нажата корректная клавиша, показываем сведения по выбранному типу.
            // TryGetValue: ищет ключ key в словаре; если нашёл - возвращает true и кладёт значение в переменную selectedType (out-параметр: метод «возвращает» значение через параметр). Не найден - false, исключения нет.
            // Type? - знак ? значит «может быть null» (nullable reference type); при true внутри if переменная точно заполнена.
            if (TypeMenu.TryGetValue(key, out Type? selectedType))
            {
                ShowTypeInfo(selectedType); // Показываем подробную информацию о выбранном типе (вложенное меню с клавишей M).
                return; // После просмотра типа возвращаемся в главное меню (а не в список типов).
            }
        } // конец цикла while в SelectType (при неверной клавише - новый виток)
    } // конец метода SelectType

    // Подробная информация об одном типе. Параметр t - выбранный тип. Используется: вызывается из SelectType. Внутри вызывает ShowMethodsByVariant (клавиша M) и JoinNames.
    private static void ShowTypeInfo(Type t)
    {
        // Цикл: экран перерисовывается после возврата из просмотра методов.
        while (true)
        {
            SafeClear(); // Чистим экран.

            // По умолчанию рефлексия возвращает только открытые элементы.
            // GetMembers() без флагов - все public-члены (методы, свойства, поля, конструкторы, вложенные типы), включая унаследованные от object. Результат - массив MemberInfo; используется для «Общее число элементов».
            MemberInfo[] members = t.GetMembers();
            MethodInfo[] methods = t.GetMethods(); // Все public-методы (экземплярные и статические, с унаследованными). Используется для «Число методов».
            PropertyInfo[] properties = t.GetProperties(); // Все public-свойства. Используется для «Число свойств» и «Список свойств».
            FieldInfo[] fields = t.GetFields(); // Все public-поля. Используется для «Число полей» и «Список полей».

            // $"..." - интерполяция строки: то, что в { }, подставляется как значение. {t} вызывает t.ToString() -> полное имя типа (например System.Int32).
            Console.WriteLine($"Информация по типу: {t}");
            // Тернарный оператор: условие ? если_да : если_нет. IsValueType == true для struct/int/char и т.п. Печатаем «+» (значимый) или «-» (ссылочный).
            Console.WriteLine($"Значимый тип: {(t.IsValueType ? "+" : "-")}");
            // ?? - «если слева null, взять справа». Namespace может быть null (например у анонимных/generic-параметров), тогда печатаем «-».
            Console.WriteLine($"Пространство имен: {t.Namespace ?? "-"}");
            // t.Assembly - сборка (dll), где объявлен тип; GetName().Name - её короткое имя (например System.Private.CoreLib).
            Console.WriteLine($"Сборка: {t.Assembly.GetName().Name}");
            Console.WriteLine($"Общее число элементов: {members.Length}"); // Длина массива members.
            Console.WriteLine($"Число методов: {methods.Length}"); // Длина массива methods.
            Console.WriteLine($"Число свойств: {properties.Length}"); // Длина массива properties.
            Console.WriteLine($"Число полей: {fields.Length}"); // Длина массива fields.
            // Лямбда f => f.Name - «для каждого поля f взять его имя»; Select превращает FieldInfo[] в последовательность строк; JoinNames склеивает их через запятую.
            Console.WriteLine($"Список полей: {JoinNames(fields.Select(f => f.Name))}");
            // То же самое для свойств: p => p.Name.
            Console.WriteLine($"Список свойств: {JoinNames(properties.Select(p => p.Name))}");
            Console.WriteLine(); // Пустая строка для читаемости.
            Console.WriteLine("Нажмите 'M' для вывода дополнительной информации по методам"); // Подсказка: обрабатывается ниже (key is 'm' or 'M').
            Console.WriteLine("Нажмите '0' для выхода в главное меню"); // Подсказка: обрабатывается ниже (key == '0').

            char key = ReadMenuKey(); // Ждём клавишу пользователя. Используется в двух if ниже.
            if (key == '0') // Клавиша 0 - выход.
            {
                return; // Выходим из ShowTypeInfo (и вернёмся в SelectType, который сразу сделает return в Main).
            }

            // Pattern matching: «key is 'm' or 'M'» - проверка, что key равен одному из двух символов (короткая замена key == 'm' || key == 'M').
            if (key is 'm' or 'M')
            {
                ShowMethodsByVariant(t); // Показываем таблицу методов, сгруппированных по имени (задание варианта 4).
            }
        } // конец цикла while в ShowTypeInfo
    } // конец метода ShowTypeInfo

    // Таблица «имя метода - число перегрузок - какие количества параметров». Используется: вызывается из ShowTypeInfo по клавише M. Это основное задание варианта 4 для выбранного типа.
    private static void ShowMethodsByVariant(Type t)
    {
        SafeClear(); // Чистим экран.
        Console.WriteLine($"Методы типа {t}"); // Заголовок: полное имя типа.
        Console.WriteLine($"Вариант {Variant}: группировка по названиям, сводка параметров в формате 0, 2, 3"); // Пояснение, подставляется константа Variant (=4).
        Console.WriteLine(); // Пустая строка.

        MethodInfo[] allMethods = t.GetMethods(); // Все public-методы типа. Используются в запросе groups ниже. Перегрузки (одноимённые методы) присутствуют отдельными элементами.

        // Вариант 4: группировка по имени метода + сводка по числу параметров (0, 2, 3).
        // LINQ простыми словами: GroupBy(m => m.Name) раскладывает методы по «корзинам» с одинаковым именем (перегрузки Add окажутся в одной корзине).
        // OrderBy(g => g.Key) сортирует корзины по имени (Key - имя метода) по алфавиту. var - компилятор сам выводит тип. Запрос ленивый: реально выполнится при обходе в foreach.
        var groups = allMethods
            .GroupBy(m => m.Name)
            .OrderBy(g => g.Key);

        // Шапка таблицы. Форматная строка: {0,-30} - 0-й аргумент, выровненный влево в поле шириной 30 символов. (Исправлено: раньше в строке был лишний {3} при трёх аргументах, из-за чего M падала с FormatException.)
        Console.WriteLine("{0,-30} {1,-18} {2,-20}",
        "Название", // Аргумент {0}: колонка «Название».
        "Число перегрузок", // Аргумент {1}: сколько методов с этим именем.
        "Число параметров"); // Аргумент {2}: какие количества параметров встречаются.

        // Обходим каждую группу (одно уникальное имя метода). Здесь LINQ-запрос groups реально выполняется.
        foreach (var group in groups)
        {
            // Для группы: Select берёт у каждого метода число параметров (GetParameters().Length); Distinct убирает повторы; OrderBy(n => n) сортирует по возрастанию; string.Join склеивает числа через «, » -> например «0, 2, 3». Результат используется в строке вывода ниже.
            string argsSummary = string.Join(", ",
                group
                    .Select(m => m.GetParameters().Length)
                    .Distinct()
                    .OrderBy(n => n));



            // Печатаем строку таблицы: имя метода (group.Key), число перегрузок (group.Count() - сколько методов в группе) и сводка параметров. (Здесь тоже убран лишний {3}.)
            Console.WriteLine("{0,-30} {1,-18} {2,-20}",
                group.Key, // {0}: имя метода.
                group.Count(), // {1}: число перегрузок.
                argsSummary); // {2}: сводка по параметрам.
        } // конец foreach по группам

        Console.WriteLine(); // Пустая строка перед подсказкой.
        Console.WriteLine("Нажмите любую клавишу, чтобы вернуться"); // Подсказка пользователю.
        WaitForAnyKey(); // Ждём любую клавишу, чтобы таблица не исчезла сразу. Потом возврат в ShowTypeInfo.
    } // конец метода ShowMethodsByVariant

    // Экран «Параметры консоли» (пункт 3 главного меню). Используется: вызывается из Main (case '3'). Вызывает TrySelectColor.
    private static void ChangeConsoleView()
    {
        // Цикл: меню повторяется, пока не нажат '0'.
        while (true)
        {
            // Экран изменения цветов консоли.
            SafeClear(); // Чистим экран (после смены цвета фона это заливает весь экран новым цветом).
            Console.WriteLine("Параметры консоли:"); // Заголовок.
            Console.WriteLine("1 - Изменить цвет шрифта"); // Клавиша 1 -> case '1' ниже.
            Console.WriteLine("2 - Изменить цвет фона"); // Клавиша 2 -> case '2' ниже.
            Console.WriteLine("0 - Выход в главное меню"); // Клавиша 0 -> return.

            // Выбор по нажатой клавише.
            switch (ReadMenuKey())
            {
                case '1': // Смена цвета шрифта.
                    // out ConsoleColor fore - метод TrySelectColor через out-параметр кладёт выбранный цвет в новую переменную fore; возвращает bool: true - цвет выбран корректно.
                    if (TrySelectColor("Выберите цвет шрифта", out ConsoleColor fore))
                    {
                        Console.ForegroundColor = fore; // Устанавливаем цвет текста консоли; действует на весь дальнейший вывод.
                        SafeClear(); // Очищаем экран, чтобы применить цвет сразу к следующему выводу.
                    }
                    break; // Выход из switch, меню перерисуется.
                case '2': // Смена цвета фона.
                    if (TrySelectColor("Выберите цвет фона", out ConsoleColor back)) // Аналогично, результат в переменной back.
                    {
                        Console.BackgroundColor = back; // Устанавливаем цвет фона консоли.
                        SafeClear(); // Clear заливает весь экран новым фоном (без Clear фон поменялся бы только под новым текстом).
                    }
                    break; // Возврат к меню параметров.
                case '0': // Назад.
                    return; // Возврат в Main -> главное меню.
            }
        } // конец цикла while в ChangeConsoleView
    } // конец метода ChangeConsoleView

    // Показывает список цветов и просит ввести номер. Возвращает true, если номер корректный, и через out-параметр selected отдаёт выбранный цвет. Используется: вызывается дважды из ChangeConsoleView (шрифт и фон).
    // title - заголовок экрана; out ConsoleColor selected - выходной параметр: метод обязан присвоить его перед любым return.
    private static bool TrySelectColor(string title, out ConsoleColor selected)
    {
        SafeClear(); // Чистим экран.
        Console.WriteLine(title); // Печатаем заголовок, переданный вызывающим кодом.
        Console.WriteLine(); // Пустая строка.

        // Enum.GetValues<ConsoleColor>() - массив всех значений перечисления ConsoleColor (Black, DarkBlue, ... White; всего 16). Индекс в массиве и будет «номером цвета» для пользователя.
        ConsoleColor[] colors = Enum.GetValues<ConsoleColor>();
        for (int i = 0; i < colors.Length; i++) // Проходим по всем цветам с номером i.
        {
            // {i,2} - число i, выровненное вправо в поле шириной 2 (чтобы номера 0-15 выстроились столбиком).
            Console.WriteLine($"{i,2} - {colors[i]}");
        } // конец цикла for по цветам

        Console.WriteLine(); // Пустая строка перед вводом.
        Console.Write("Введите номер цвета: "); // Write (без перевода строки), чтобы ввод шёл на той же строке.
        string? input = Console.ReadLine(); // Читаем строку до Enter; string? - может быть null (например, конец входного потока).

        // Валидируем индекс, чтобы не получить исключение при выборе цвета.
        // int.TryParse пытается превратить текст в число; при успехе кладёт его в out int idx и возвращает true (при неудаче исключения не будет). Затем проверяем диапазон 0..Length-1. && - «и», вычисляется слева направо и прекращается на первом false.
        if (int.TryParse(input, out int idx) && idx >= 0 && idx < colors.Length)
        {
            selected = colors[idx]; // Записываем выбранный цвет в out-параметр (вызывающий получит его как fore/back).
            return true; // Сообщаем: цвет выбран.
        }

        selected = default; // Обязательное присвоение out-параметра при неудаче: default для enum - значение 0 (Black). Вызывающий код его не использует, т.к. получит false.
        return false; // Сообщаем: ввод некорректный, цвет не менять.
    } // конец метода TrySelectColor

    // Экран «Общая информация по типам» (пункт 1 главного меню). Используется: вызывается из Main (case '1'). Здесь вызываются все три метода варианта 4 из TypeUtils.cs.
    private static void ShowAllTypeInfo()
    {
        SafeClear(); // Чистим экран.

        // Собираем типы из всех уже загруженных в домен приложений сборок.
        List<Type> allTypes = GetAllTypesFromLoadedAssemblies(); // Список всех типов из всех сборок. Используется: в подсчётах ниже и передаётся в методы TypeUtils.
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies(); // Массив загруженных сборок. Используется: только для вывода «Подключенные сборки: N».

        // Лямбда t => t.IsClass: «для типа t верни true, если это класс». Count с условием считает подходящие элементы. IsClass == true для ссылочных типов-классов (в т.ч. record class), но не для интерфейсов, структур и enum.
        int classCount = allTypes.Count(t => t.IsClass);
        int valueCount = allTypes.Count(t => t.IsValueType); // Сколько значимых типов (struct, enum, int и т.д.).

        Console.WriteLine("Общая информация по типам"); // Заголовок экрана.
        Console.WriteLine($"Подключенные сборки: {assemblies.Length}"); // Число загруженных сборок.
        Console.WriteLine($"Всего типов по всем подключенным сборкам: {allTypes.Count}"); // Число типов (у List - свойство Count).
        Console.WriteLine($"Ссылочные типы (только классы): {classCount}"); // Результат classCount.
        Console.WriteLine($"Значимые типы: {valueCount}"); // Результат valueCount.
        Console.WriteLine(); // Пустая строка.
        Console.WriteLine($"Информация в соответствии с вариантом V = {Variant}"); // Подставляется константа Variant (4).

        // Дополнительные вычисления по варианту 4 вынесены в класс TypeUtils.
        MethodInfo capsMethod = TypeUtils.GetMethodWithManyCaps(allTypes); // Метод с наибольшим числом заглавных букв в имени (TypeUtils.cs). Результат идёт в строку «Метод с наибольшим...» ниже.
        Type typeWithPrivateFields = TypeUtils.GetTypeWithMaxPrivateFields(allTypes); // Тип с наибольшим числом private-полей. Результат идёт в следующую строку вывода.
        // out List<Type> selectedTypes - объявляем переменную прямо в вызове; метод заполнит её списком подходящих типов, а через return вернёт их количество.
        int withPublicFields = TypeUtils.CountTypesWithPublicFields(allTypes, out List<Type> selectedTypes);

        Console.WriteLine($"Метод с наибольшим количеством букв в верхнем регистре: {GetMethodDisplay(capsMethod)}"); // GetMethodDisplay делает читаемую строку «Тип.Метод».
        Console.WriteLine($"Тип с наибольшим числом закрытых полей: {GetTypeDisplay(typeWithPrivateFields)}"); // GetTypeDisplay - полное имя типа.
        Console.WriteLine($"Количество типов с открытыми полями (нестатические, неконстанты): {withPublicFields}"); // Число из return CountTypesWithPublicFields.
        // Take(10) - взять только первые 10 типов; Select(GetTypeDisplay) - превратить каждый в строку (передан метод вместо лямбды, «группа методов»); JoinNames - склеить через запятую.
        Console.WriteLine($"Примеры типов: {JoinNames(selectedTypes.Take(10).Select(GetTypeDisplay))}");

        Console.WriteLine(); // Пустая строка.
        Console.WriteLine("Нажмите любую клавишу, чтобы вернуться в главное меню"); // Подсказка.
        WaitForAnyKey(); // Пауза до нажатия клавиши, потом возврат в Main.
    } // конец метода ShowAllTypeInfo

    // Собирает все типы из всех загруженных сборок. Используется: вызывается из ShowAllTypeInfo (результат - allTypes, который дальше идёт в TypeUtils).
    private static List<Type> GetAllTypesFromLoadedAssemblies()
    {
        Assembly[] refAssemblies = AppDomain.CurrentDomain.GetAssemblies(); // Все сборки, загруженные в текущий процесс на данный момент (сборка = dll/exe с типами).
        List<Type> types = new(); // Пустой список-накопитель; new() - тип выводится из объявления слева.

        foreach (Assembly asm in refAssemblies) // Идём по сборкам по одной.
        {
            try // try/catch нужен, потому что GetTypes() иногда бросает исключение.
            {
                types.AddRange(asm.GetTypes()); // GetTypes() - все типы сборки; AddRange добавляет их в общий список.
            }
            catch (ReflectionTypeLoadException ex) // Исключение «часть типов сборки не удалось загрузить» (например, нет зависимой dll).
            {
                // Если часть типов не загрузилась, используем доступные типы из исключения.
                // ex.Types - массив, где успешно загруженные типы, а неудачные - null. Where отбрасывает null; «!» в конце говорит компилятору «после фильтра null нет» (гасит предупреждение nullable).
                types.AddRange(ex.Types.Where(t => t is not null)!);
            }
        } // конец foreach по сборкам

        // Убираем дубликаты: один и тот же Type может встречаться повторно.
        // Distinct() оставляет уникальные элементы, ToList() превращает результат обратно в List<Type> (Distinct сам ленивый).
        return types.Distinct().ToList();
    } // конец метода GetAllTypesFromLoadedAssemblies

    // Склеивает имена в одну строку через запятую или возвращает «-», если имён нет. Используется: в ShowTypeInfo (список полей и свойств) и в ShowAllTypeInfo (примеры типов).
    private static string JoinNames(IEnumerable<string> names)
    {
        // Пустые значения отбрасываются, чтобы в выводе не было "мусора".
        // Where с лямбдой оставляет только непустые строки; ToArray() превращает результат в массив (выполняет запрос).
        string[] items = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();
        // Если элементов нет - «-», иначе string.Join соединяет их через «, ». Тернарный оператор ?: - «если да : если нет».
        return items.Length == 0 ? "-" : string.Join(", ", items);
    } // конец метода JoinNames

    // Короткая запись метода через =>. Возвращает полное имя типа (с пространством имён); если FullName равно null (бывает у generic-параметров), берёт короткое Name. Используется: в ShowAllTypeInfo (тип с private-полями, примеры типов) и в GetMethodDisplay.
    private static string GetTypeDisplay(Type t) => t.FullName ?? t.Name;

    // Формирует строку «Пространство.Тип.ИмяМетода». Используется: в ShowAllTypeInfo для вывода метода с наибольшим числом заглавных букв.
    // method.DeclaringType - тип, где метод объявлен (может быть null, тогда подставляем typeof(object) через ??).
    private static string GetMethodDisplay(MethodInfo method) =>
        $"{GetTypeDisplay(method.DeclaringType ?? typeof(object))}.{method.Name}";

    // Читает одну «клавишу» меню: в обычной консоли - реальное нажатие без Enter, при перенаправленном вводе - первый символ строки. Используется: в Main, SelectType, ShowTypeInfo, ChangeConsoleView (везде, где меню).
    private static char ReadMenuKey()
    {
        if (!Console.IsInputRedirected) // IsInputRedirected == true, если ввод идёт не с клавиатуры (пайп/файл, например при автотесте). Нормальный запуск - false, входим в блок.
        {
            try
            {
                // Читаем одну клавишу без отображения в консоли.
                // ReadKey(true): true = не печатать символ на экране; .KeyChar - символ клавиши; return сразу отдаёт его вызывающему.
                return Console.ReadKey(true).KeyChar;
            }
            catch (InvalidOperationException) // Если ReadKey всё же недоступен - переходим к запасному варианту ниже.
            {
            }
        }

        // Режим с перенаправленным вводом (например, пайп из файла/скрипта).
        string? line = Console.ReadLine(); // Читаем целую строку; может вернуть null при конце ввода.
        // Пустая строка/null -> символ '\0' (нулевой, не совпадает ни с одним пунктом меню); иначе берём первый символ строки.
        return string.IsNullOrEmpty(line) ? '\0' : line[0];
    } // конец метода ReadMenuKey

    // Ждёт любую клавишу (пауза, чтобы пользователь успел прочитать экран). Используется: в ShowAllTypeInfo и ShowMethodsByVariant.
    private static void WaitForAnyKey()
    {
        if (!Console.IsInputRedirected) // Обычная консоль: ввод с клавиатуры.
        {
            try
            {
                Console.ReadKey(true); // Ждём любую клавишу, не показывая её.
                return; // Клавиша нажата - выходим из метода.
            }
            catch (InvalidOperationException) // Если ReadKey недоступен - идём к запасному варианту.
            {
            }
        }

        // В режиме перенаправления "любая клавиша" имитируется чтением строки.
        Console.ReadLine(); // Читаем и выбрасываем строку - это и есть «пауза» при вводе из пайпа.
    } // конец метода WaitForAnyKey

    // Очистка консоли, которая не роняет программу. Используется: в начале каждого экрана (ShowMainMenu, SelectType, ShowTypeInfo, ShowMethodsByVariant, ChangeConsoleView, TrySelectColor, ShowAllTypeInfo).
    private static void SafeClear()
    {
        try // Console.Clear() может бросить исключение, если консоли нет (вывод перенаправлен).
        {
            Console.Clear(); // Стирает экран и ставит курсор в левый верхний угол.
        }
        catch (IOException) // Ошибка ввода-вывода при очистке.
        {
            // Игнорируем ошибки очистки экрана в средах, где Console.Clear недоступен.
        }
        catch (InvalidOperationException) // Консольный дескриптор недоступен.
        {
            // Аналогично: не прерываем программу из-за невозможности очистить экран.
        }
    } // конец метода SafeClear
} // конец класса Program

// Тестовый record-тип для пункта меню "record".
// record - краткий синтаксис класса с неизменяемыми свойствами Id и Name: компилятор сам создаёт конструктор, свойства, ToString, Equals, GetHashCode, ==, != и Deconstruct. Ссылочный тип (IsClass == true).
// Используется: в словаре TypeMenu (ключ '8') - его Type анализируется рефлексией при выборе пункта 8; в остальном коде не создаётся.
public record DemoRecord(int Id, string Name); // Id и Name - параметры первичного конструктора, из них компилятор делает открытые свойства.
