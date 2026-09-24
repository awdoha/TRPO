using System.Reflection; // Классы рефлексии: BindingFlags, MethodInfo, FieldInfo. Используются во всех трёх методах этого файла. (Linq и Collections подключены неявно через ImplicitUsings в Lab1.csproj.)

namespace Lab1; // Файловое пространство имён Lab1 - то же, что в Program.cs, поэтому Program вызывает TypeUtils без using.

// Статический класс с тремя методами задания варианта 4. Используется: все три метода вызываются из Program.ShowAllTypeInfo (Program.cs, пункт 1 главного меню).
public static class TypeUtils
{
    // Единый набор флагов: открытые/закрытые, статические/экземплярные,
    // только объявленные в самом типе (без унаследованных).
    // BindingFlags - перечисление-«битовая маска»: флаги объединяются оператором | (побитовое ИЛИ) и говорят рефлексии, ЧТО искать. Без флагов GetMethods/GetFields ищут только public.
    // Используется: в GetMethodWithManyCaps (type.GetMethods) и в GetTypeWithMaxPrivateFields (type.GetFields). const - значение вычисляется при компиляции.
    private const BindingFlags AllDeclaredMembers =
        BindingFlags.Public | // Искать открытые (public) члены.
        BindingFlags.NonPublic | // Искать неоткрытые (private, protected, internal) члены; без этого флага их не найти.
        BindingFlags.Instance | // Искать члены экземпляра (обычные, не static).
        BindingFlags.Static | // Искать статические члены; без Instance/Static одновременно поиск не вернёт ничего.
        BindingFlags.DeclaredOnly; // Только объявленные в самом типе, без унаследованных (иначе у каждого типа «нашлись» бы члены object и результаты исказились).

    // Вариант 4, задача 1: найти метод с наибольшим числом заглавных букв в имени среди ВСЕХ переданных типов.
    // Используется: вызывается из Program.ShowAllTypeInfo (результат capsMethod печатается в строке «Метод с наибольшим количеством букв в верхнем регистре»).
    // Параметр types - список всех типов из всех загруженных сборок; возвращается MethodInfo - описание найденного метода.
    public static MethodInfo GetMethodWithManyCaps(List<Type> types)
    {
        // Ищем метод, в имени которого больше всего заглавных букв.
        // При равенстве выбираем метод с более длинным именем.
        MethodInfo? best = null; // Лучший найденный на данный момент метод; null (знак ?) - пока ничего не найдено. Используется: обновляется в цикле, возвращается в конце.
        int bestCaps = -1; // Наибольшее число заглавных букв, найденное ранее. -1 (а не 0), чтобы первый же метод, даже без заглавных, оказался «лучше» и записался.
        int bestNameLength = -1; // Длина имени лучшего метода; нужна для разрешения ничьей по заглавным буквам.

        foreach (Type type in types) // Внешний цикл: перебираем все типы по одному.
        {
            if (type is null) // Защитная проверка: в списке мог оказаться null.
            {
                continue; // Пропускаем этот элемент и идём к следующему типу.
            }

            MethodInfo[] methods; // Сюда попадут методы текущего типа. Объявлена заранее, чтобы использовать и внутри try, и после него.
            try
            {
                methods = type.GetMethods(AllDeclaredMembers); // Рефлексия: получить все методы типа по набору флагов AllDeclaredMembers (любой доступ, static и нет, только свои).
            }
            catch
            {
                // Некоторые типы могут выбрасывать исключения при рефлексии.
                continue; // Если тип «капризный» - пропускаем его целиком, не роняя программу.
            }

            foreach (MethodInfo method in methods) // Внутренний цикл: перебираем методы текущего типа.
            {
                // method.Name - строка-имя; Count(char.IsUpper) - LINQ считает символы, для которых метод char.IsUpper вернул true (то есть заглавные буквы). char.IsUpper передан как «группа методов» вместо лямбды.
                int capsCount = method.Name.Count(char.IsUpper);
                int nameLength = method.Name.Length; // Длина имени - для правила «при равенстве побеждает более длинное имя».

                // Новый рекорд, если заглавных больше, ИЛИ (заглавных столько же И имя длиннее). || - «или», && - «и».
                if (capsCount > bestCaps || (capsCount == bestCaps && nameLength > bestNameLength))
                {
                    best = method; // Запоминаем метод как текущего лидера.
                    bestCaps = capsCount; // Обновляем рекорд заглавных букв.
                    bestNameLength = nameLength; // Обновляем длину имени лидера.
                }
            } // конец внутреннего foreach (методы типа)
        } // конец внешнего foreach (типы)

        // Защита от пустого результата, чтобы метод всегда возвращал корректный MethodInfo.
        // ?? - если best равен null (метод не найден), берём запасной: у object метод «ToString» (nameof(ToString) даёт строку "ToString"). Знак ! сообщает компилятору, что GetMethod точно не вернёт null.
        return best ?? typeof(object).GetMethod(nameof(ToString))!;
    } // конец метода GetMethodWithManyCaps

    // Вариант 4, задача 2: найти тип с наибольшим числом закрытых (private) полей.
    // Используется: вызывается из Program.ShowAllTypeInfo (результат typeWithPrivateFields печатается в строке «Тип с наибольшим числом закрытых полей»).
    public static Type GetTypeWithMaxPrivateFields(List<Type> types)
    {
        Type? bestType = null; // Лучший тип на данный момент; null - пока не найден. Возвращается в конце.
        int maxPrivateFields = -1; // Максимум private-полей. -1, потому что у типа может быть 0 таких полей - начальное значение должно быть меньше любого реального.

        foreach (Type type in types) // Перебираем все типы.
        {
            if (type is null) // Защита от null в списке.
            {
                continue; // Пропускаем.
            }

            FieldInfo[] fields; // Поля текущего типа; объявлена до try, чтобы использовать после него.
            try
            {
                fields = type.GetFields(AllDeclaredMembers); // Все объявленные в типе поля любого доступа (флаги из константы выше).
            }
            catch
            {
                continue; // Рефлексия над этим типом не удалась - пропускаем.
            }

            // Считаем только поля с модификатором private.
            // Лямбда f => f.IsPrivate: «для поля f верни true, если оно private». Count считает такие поля.
            int privateCount = fields.Count(f => f.IsPrivate);
            if (privateCount > maxPrivateFields) // Строго больше: при равенстве остаётся первый найденный тип.
            {
                maxPrivateFields = privateCount; // Обновляем максимум.
                bestType = type; // Запоминаем тип-лидер.
            }
        } // конец цикла foreach по типам

        return bestType ?? typeof(object); // Если тип не найден (пустой список) - вернуть запасной typeof(object), чтобы результат никогда не был null.
    } // конец метода GetTypeWithMaxPrivateFields

    // Вариант 4, задача 3: посчитать типы, у которых есть открытые нестатические не-константные поля, и отдать сам список таких типов.
    // Используется: вызывается из Program.ShowAllTypeInfo; возвращаемое число печатается как «Количество типов с открытыми полями», а список selectedTypes - как «Примеры типов» (первые 10).
    // out List<Type> selectedTypes - выходной параметр: метод обязан присвоить его и так «возвращает» второй результат помимо int.
    public static int CountTypesWithPublicFields(List<Type> types, out List<Type> selectedTypes)
    {
        // Возвращаем и количество, и сами найденные типы через out-параметр.
        selectedTypes = new List<Type>(); // Создаём пустой список и сразу отдаём его вызывающему через out; дальше заполняем.
        // Локальная константа: только public, только экземплярные, только объявленные в самом типе. Отдельного флага NonPublic и Static нет - поэтому статические и закрытые поля не попадут. Используется: в type.GetFields ниже.
        const BindingFlags PublicInstanceDeclared =
            BindingFlags.Public | // Только открытые.
            BindingFlags.Instance | // Только нестатические.
            BindingFlags.DeclaredOnly; // Только объявленные в самом типе.

        foreach (Type type in types) // Перебираем все типы.
        {
            if (type is null) // Защита от null.
            {
                continue; // Пропускаем.
            }

            FieldInfo[] fields; // Подходящие поля текущего типа.
            try
            {
                fields = type.GetFields(PublicInstanceDeclared); // Рефлексия: получаем открытые экземплярные поля типа (без унаследованных).
            }
            catch
            {
                continue; // При ошибке рефлексии тип пропускаем.
            }

            // По условию считаются только открытые нестатические поля и не константы.
            // Флаг Instance уже исключает статические поля, поэтому дополнительно
            // отбрасываем только literal (const) поля.
            // Any(f => !f.IsLiteral): «есть ли хотя бы одно поле, которое НЕ является const-литералом». IsLiteral == true для const-полей; ! - отрицание. Результат кладётся в bool.
            bool hasPublicNonConstField = fields.Any(f => !f.IsLiteral);
            if (hasPublicNonConstField) // Если такое поле есть, тип подходит.
            {
                selectedTypes.Add(type); // Добавляем тип в список-результат (он уйдёт через out-параметр).
            }
        } // конец цикла foreach по типам

        return selectedTypes.Count; // Количество найденных типов (свойство Count списка) - основной результат через return.
    } // конец метода CountTypesWithPublicFields
} // конец класса TypeUtils
