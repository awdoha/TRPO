namespace MusicCatalog.Utils; // пространство имён вспомогательных классов; подключается в Program.cs строкой using MusicCatalog.Utils

// Статический класс-утилита (объекты не создаются, только вызов VariantCalculator.Compute). Используется: Program.Main
public static class VariantCalculator
{
    // Формула из задания: V = (K1 + K2) % 11,
    // где K1 и K2 — первые буквы фамилии и имени (заглавные, русские).
    // Метод считает номер варианта. Используется: Program.Main вызывает Compute('К', 'Д') -> результат 4
    public static int Compute(char k1, char k2)
    {
        // char при сложении превращается в число (код Unicode: 'К'=1050, 'Д'=1044); % 11 — остаток от деления на 11; результат уходит в Program.Main
        return (k1 + k2) % 11;
    } // конец метода Compute
} // конец класса VariantCalculator
