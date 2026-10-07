using System.Globalization;
using System.Text;

namespace WarehouseManager;

internal class Program
{
    static void Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        Dictionary<string, Product> warehouse = new();

        // CSV копируется рядом с программой при сборке.
        string path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "products.csv");
        Console.WriteLine("УЧЁТ ОСТАТКОВ ТОВАРОВ НА СКЛАДЕ");
        if (!InitializeWarehouse(warehouse, path)) return;
        Console.WriteLine($"Загружено товаров: {warehouse.Count}");

        while (true)
        {
            Console.WriteLine("\n1 — Поиск товара");
            Console.WriteLine("2 — Приход товара");
            Console.WriteLine("3 — Расход (продажа) товара");
            Console.WriteLine("0 — Выход");
            Console.Write("Выберите действие: ");
            string? choice = Console.ReadLine();
            if (choice is null || choice == "0") break;
            switch (choice)
            {
                case "1": FindProduct(warehouse); break;
                case "2": ReceiveProduct(warehouse); break;
                case "3": SellProduct(warehouse); break;
                default: Console.WriteLine("Ошибка: введите 0, 1, 2 или 3."); break;
            }
        }
        Console.WriteLine("Работа завершена.");
    }

    static bool InitializeWarehouse(Dictionary<string, Product> warehouse, string path)
    {
        try
        {
            int lineNumber = 0;
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] fields = line.Split(';');
                if (fields.Length != 4)
                {
                    Console.WriteLine($"Строка {lineNumber}: нужны 4 поля через ';'. Строка пропущена.");
                    continue;
                }
                string article = fields[0].Trim();
                string name = fields[1].Trim();
                if (article.Length == 0 || name.Length == 0 ||
                    !decimal.TryParse(fields[2].Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out decimal price) || price < 0 ||
                    !int.TryParse(fields[3].Trim(), out int quantity) || quantity < 0)
                {
                    Console.WriteLine($"Строка {lineNumber}: неверные данные товара. Строка пропущена.");
                    continue;
                }
                if (warehouse.ContainsKey(article))
                {
                    Console.WriteLine($"Строка {lineNumber}: артикул {article} уже существует. Строка пропущена.");
                    continue;
                }
                warehouse.Add(article, new Product
                {
                    Article = article,
                    Name = name,
                    Price = price,
                    Quantity = quantity
                });
            }
            if (warehouse.Count == 0)
            {
                Console.WriteLine("Нет корректных товаров для загрузки. Проверьте CSV-файл.");
                return false;
            }
            return true;
        }
        catch (IOException)
        {
            Console.WriteLine($"Не удалось прочитать файл: {path}");
            Console.WriteLine("Проверьте наличие products.csv и закройте программы, блокирующие файл.");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Нет доступа к CSV-файлу.");
            return false;
        }
    }

    static Product? GetProduct(Dictionary<string, Product> warehouse)
    {
        Console.Write("Введите артикул: ");
        string? article = Console.ReadLine()?.Trim();
        if (article is null) return null;
        // Поиск по ключу хеш-таблицы, а не по номеру товара в списке.
        if (warehouse.TryGetValue(article, out Product? product)) return product;
        Console.WriteLine("Ошибка: товар с таким артикулом не найден.");
        return null;
    }

    static void PrintProduct(Product product)
    {
        Console.WriteLine($"Артикул: {product.Article}");
        Console.WriteLine($"Название: {product.Name}");
        Console.WriteLine($"Цена: {product.Price.ToString("F2", CultureInfo.GetCultureInfo("ru-RU"))} руб.");
        Console.WriteLine($"Текущий остаток: {product.Quantity} шт.");
    }

    static void FindProduct(Dictionary<string, Product> warehouse)
    {
        Product? product = GetProduct(warehouse);
        if (product is not null) PrintProduct(product);
    }

    static int? ReadQuantity()
    {
        while (true)
        {
            Console.Write("Введите количество единиц: ");
            string? input = Console.ReadLine();
            if (input is null) return null;
            if (int.TryParse(input, out int quantity) && quantity > 0) return quantity;
            Console.WriteLine("Ошибка: введите положительное целое число.");
        }
    }

    static void ReceiveProduct(Dictionary<string, Product> warehouse)
    {
        Product? product = GetProduct(warehouse);
        if (product is null) return;
        int? quantity = ReadQuantity();
        if (quantity is null) return;
        if (quantity.Value > int.MaxValue - product.Quantity)
        {
            Console.WriteLine("Приход отменён: итоговый остаток слишком большой.");
            return;
        }
        product.Quantity += quantity.Value;
        Console.WriteLine($"Приход выполнен: +{quantity.Value} шт.");
        PrintProduct(product);
    }

    static void SellProduct(Dictionary<string, Product> warehouse)
    {
        Product? product = GetProduct(warehouse);
        if (product is null) return;
        int? quantity = ReadQuantity();
        if (quantity is null) return;
        if (quantity.Value > product.Quantity)
        {
            Console.WriteLine($"Недостаточно товара: доступно {product.Quantity} шт. Продажа заблокирована.");
            Console.WriteLine("Остаток не изменён.");
            return;
        }
        product.Quantity -= quantity.Value;
        Console.WriteLine($"Продажа выполнена: -{quantity.Value} шт.");
        PrintProduct(product);
    }
}
