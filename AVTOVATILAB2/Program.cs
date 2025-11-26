using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RegexAutomaton
{
    // Класс для представления регулярного выражения
    public abstract class RegexSt
    {
        public abstract IEnumerable<string> GenerateWords(HashSet<char> alphabet);
    }

    // Буква алфавита
    public class CharRegularSt : RegexSt
    {
        public char Value { get; }

        public CharRegularSt(char value)
        {
            Value = value;
        }

        public override IEnumerable<string> GenerateWords(HashSet<char> alphabet)
        {
            yield return Value.ToString();
        }
    }

    // Конкатенация
    public class CharConcat : RegexSt
    {
        public RegexSt Left { get; }
        public RegexSt Right { get; }

        public CharConcat(RegexSt left, RegexSt right)
        {
            Left = left;
            Right = right;
        }

        public override IEnumerable<string> GenerateWords(HashSet<char> alphabet)
        {
            foreach (var leftWord in Left.GenerateWords(alphabet))
            {
                foreach (var rightWord in Right.GenerateWords(alphabet))
                {
                    yield return leftWord + rightWord;
                }
            }
        }
    }

    // Объединение (или)
    public class Union : RegexSt
    {
        public RegexSt Left { get; }
        public RegexSt Right { get; }

        public Union(RegexSt left, RegexSt right)
        {
            Left = left;
            Right = right;
        }

        public override IEnumerable<string> GenerateWords(HashSet<char> alphabet)
        {
            foreach (var word in Left.GenerateWords(alphabet))
            {
                yield return word;
            }

            foreach (var word in Right.GenerateWords(alphabet))
            {
                yield return word;
            }
        }
    }

    // Звезда Клини (повторение 0 или более раз)
    public class StarNode : RegexSt
    {
        public RegexSt Inner { get; }

        public StarNode(RegexSt inner)
        {
            Inner = inner;
        }

        public override IEnumerable<string> GenerateWords(HashSet<char> alphabet)
        {
            // Бесконечная генерация: сначала пустое слово, затем слова возрастающей длины
            yield return "";

            // Генерируем слова все большей длины
            var currentLevel = new List<string> { "" };

            while (true)
            {
                var nextLevel = new List<string>();
                foreach (var prefix in currentLevel)
                {
                    foreach (var innerWord in Inner.GenerateWords(alphabet))
                    {
                        var newWord = prefix + innerWord;
                        yield return newWord;
                        nextLevel.Add(newWord);
                    }
                }
                currentLevel = nextLevel;
            }
        }
    }

    // Парсер регулярных выражений
    public class RegexParser
    {
        private string input;
        private int position;
        private HashSet<char> alphabet;

        public RegexParser(HashSet<char> alphabet)
        {
            this.alphabet = alphabet;
        }

        public RegexSt Parse(string regex)
        {
            input = regex;
            position = 0;
            return ParseUnion();
        }

        private RegexSt ParseUnion()
        {
            var left = ParseConcat();

            if (position < input.Length && input[position] == '+')
            {
                position++;
                var right = ParseUnion();
                return new Union(left, right);
            }

            return left;
        }

        private RegexSt ParseConcat()
        {
            var left = ParseStar();

            while (position < input.Length && input[position] != '+' && input[position] != ')')
            {
                var right = ParseStar();
                left = new CharConcat(left, right);
            }

            return left;
        }

        private RegexSt ParseStar()
        {
            var node = ParsePrimary();

            while (position < input.Length && input[position] == '*')
            {
                position++;
                node = new StarNode(node);
            }

            return node;
        }

        private RegexSt ParsePrimary()
        {
            if (position >= input.Length)
                throw new ArgumentException("Неожиданный конец регулярного выражения");

            char current = input[position];

            if (current == '(')
            {
                position++;
                var node = ParseUnion();
                if (position >= input.Length || input[position] != ')')
                    throw new ArgumentException("Ожидается закрывающая скобка");
                position++;
                return node;
            }
            else if (current == '@') // Пустое слово
            {
                position++;
                return new CharRegularSt('\0');
            }
            else if (alphabet.Contains(current))
            {
                position++;
                return new CharRegularSt(current);
            }
            else
            {
                throw new ArgumentException($"Неизвестный символ: {current}");
            }
        }
    }

    // Компаратор для лексикографического порядка
    public class LexicographicSorter : IComparer<string>
    {
        private List<char> alphabetOrder;

        public LexicographicSorter(IEnumerable<char> alphabet)
        {
            // Сохраняем порядок символов как они заданы в алфавите
            alphabetOrder = alphabet.ToList();
        }

        public int Compare(string x, string y)
        {
            // Пустая строка всегда меньше любой непустой
            if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y)) return 0;
            if (string.IsNullOrEmpty(x)) return -1;
            if (string.IsNullOrEmpty(y)) return 1;

            // Сравниваем посимвольно
            int minLength = Math.Min(x.Length, y.Length);
            for (int i = 0; i < minLength; i++)
            {
                int xIndex = alphabetOrder.IndexOf(x[i]);
                int yIndex = alphabetOrder.IndexOf(y[i]);

                if (xIndex < yIndex) return -1;
                if (xIndex > yIndex) return 1;
                // Если символы равны, переходим к следующему
            }

            // Если первые minLength символов совпадают, то короче слово идет первым
            return x.Length.CompareTo(y.Length);
        }
    }

    // Основной класс программы
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== Генератор слов по регулярному выражению ===");
            Console.ResetColor();
            bool work = true;

            while (work)
            {
                try
                {
                    // Ввод алфавита
                    Console.Write("Введите алфавит (символы без пробелов, например: abc): ");
                    string alphabetInput = Console.ReadLine();

                    if (string.IsNullOrEmpty(alphabetInput))
                    {
                        Console.ForegroundColor= ConsoleColor.Red;
                        Console.WriteLine("Алфавит не может быть пустым!");
                        Console.ResetColor();
                        return;
                    }

                    var alphabet = new HashSet<char>();
                    foreach (char c in alphabetInput)
                    {
                        if (c == '+' || c == '*' || c == '(' || c == ')' || c == '@')
                        {
                            Console.WriteLine($"Предупреждение: символ '{c}' является служебным и будет использоваться в регулярных выражениях");
                        }
                        alphabet.Add(c);
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    // Ввод регулярного выражения
                    Console.WriteLine("\nСимволы операций для регулярного выражения:");
                    Console.WriteLine("  - Символы алфавита: любые символы из введенного алфавита");
                    Console.WriteLine("  - Конкатенация: ab (a затем b)");
                    Console.WriteLine("  - Объединение: a|b (a или b)");
                    Console.WriteLine("  - Звезда Клини: a* (0 или более a)");
                    Console.WriteLine("  - Группировка: (a|b)c");
                    Console.WriteLine("  - Пустое слово: @");
                    Console.ResetColor();

                    Console.Write("\nВведите регулярное выражение: ");
                    string regex = Console.ReadLine();

                    if (string.IsNullOrEmpty(regex))
                    {
                        Console.WriteLine("Регулярное выражение не может быть пустым!");
                        return;
                    }

                    // Ввод количества итераций (слов)
                    Console.Write("Введите количество итераций (слов для вывода): ");
                    if (!int.TryParse(Console.ReadLine(), out int iterations) || iterations <= 0)
                    {
                        Console.WriteLine("Некорректное количество итераций!");
                        return;
                    }

                    // Парсинг и генерация слов
                    var parser = new RegexParser(alphabet);
                    var ast = parser.Parse(regex);
                    var words = ast.GenerateWords(alphabet).Take(iterations).ToList();

                    // Сортировка в лексикографическом порядке
                    var comparer = new LexicographicSorter(alphabetInput);
                    words.Sort(comparer);

                    // Вывод результатов
                    Console.ForegroundColor= ConsoleColor.Yellow;
                    Console.WriteLine($"\n=== Первые {words.Count} слов по регулярному выражению ===");
                    Console.ResetColor();

                    int count = 0;
                    foreach (var word in words)
                    {
                        Console.WriteLine($"{++count,3}: {(word == "" ? "@" : word)}");
                    }

                    // Дополнительная информация
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\nДополнительная информация:");
                    Console.WriteLine($"  - Алфавит: {string.Join("", alphabet)}");
                    Console.WriteLine($"  - Регулярное выражение: {regex}");
                    Console.WriteLine($"  - Количество итераций (слов): {iterations}");
                    Console.ResetColor();

                    if (words.Count < iterations)
                    {
                        Console.WriteLine($"  - Предупреждение: сгенерировано только {words.Count} слов");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nПродолжить? (1 - Да; 2 - Нет)");
                Console.ResetColor();
                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        break;

                    case "2":
                        work = false;
                        break;

                    default:
                        break;
                }
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nЗавершение работы программы...");
            Console.ResetColor();
        }
    }
}