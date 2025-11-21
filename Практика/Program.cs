using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

// Классы Point, Driver, Order остаются такими же как в предыдущем примере
public class Point
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public int CalculateManhattanDistance(Point other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }
    
    public override string ToString() => $"({X}, {Y})";
}

public class Driver
{
    public string Id { get; set; }
    public Point Location { get; set; }
    public bool IsAvailable { get; set; }
    
    public Driver(string id, Point location, bool isAvailable = true)
    {
        Id = id;
        Location = location;
        IsAvailable = isAvailable;
    }
    
    public override string ToString() => $"Driver {Id} at {Location}";
}

public class Order
{
    public string Id { get; set; }
    public Point PickupLocation { get; set; }
    
    public Order(string id, Point pickupLocation)
    {
        Id = id;
        PickupLocation = pickupLocation;
    }
    
    public override string ToString() => $"Order {Id} at {PickupLocation}";
}

// Класс с различными алгоритмами поиска
public class DriverFinder
{
    private readonly List<Driver> _drivers;
    
    public DriverFinder(List<Driver> drivers)
    {
        _drivers = drivers;
    }
    
    // Алгоритм 1: Сортировка всего списка (самый простой)
    [Benchmark]
    public List<Driver> FindNearestDrivers_SortAll(Order order)
    {
        return _drivers
            .Where(d => d.IsAvailable)
            .Select(d => new { Driver = d, Distance = d.Location.CalculateManhattanDistance(order.PickupLocation) })
            .OrderBy(x => x.Distance)
            .Take(5)
            .Select(x => x.Driver)
            .ToList();
    }
    
    // Алгоритм 2: Использование PriorityQueue (более эффективный)
    [Benchmark]
    public List<Driver> FindNearestDrivers_PriorityQueue(Order order)
    {
        var availableDrivers = _drivers.Where(d => d.IsAvailable);
        var pq = new PriorityQueue<Driver, int>();
        
        foreach (var driver in availableDrivers)
        {
            var distance = driver.Location.CalculateManhattanDistance(order.PickupLocation);
            pq.Enqueue(driver, distance);
        }
        
        var result = new List<Driver>();
        for (int i = 0; i < 5 && pq.Count > 0; i++)
        {
            result.Add(pq.Dequeue());
        }
        
        return result;
    }
    
    // Алгоритм 3: Частичная сортировка с помощью Select
    [Benchmark]
    public List<Driver> FindNearestDrivers_PartialSort(Order order)
    {
        var availableDrivers = _drivers.Where(d => d.IsAvailable).ToList();
        
        // Используем подход с поиском k наименьших элементов
        var distances = availableDrivers
            .Select(d => new { Driver = d, Distance = d.Location.CalculateManhattanDistance(order.PickupLocation) })
            .ToList();
        
        // Частичная сортировка - берем 5 минимальных
        var nearest = distances
            .OrderBy(x => x.Distance)
            .Take(5)
            .Select(x => x.Driver)
            .ToList();
            
        return nearest;
    }
    
    // Алгоритм 4: Ручная реализация с поддержанием списка топ-5
    [Benchmark]
    public List<Driver> FindNearestDrivers_ManualTop5(Order order)
    {
        var availableDrivers = _drivers.Where(d => d.IsAvailable);
        var top5 = new List<(Driver Driver, int Distance)>();
        
        foreach (var driver in availableDrivers)
        {
            var distance = driver.Location.CalculateManhattanDistance(order.PickupLocation);
            
            // Если в топ-5 меньше 5 элементов, просто добавляем
            if (top5.Count < 5)
            {
                top5.Add((driver, distance));
                top5 = top5.OrderBy(x => x.Distance).ToList();
            }
            else
            {
                //// Если текущий водитель ближе, чем самый дальний в топ-5
                if (distance < top5[4].Distance)
                {
                    top5[4] = (driver, distance);
                    top5 = top5.OrderBy(x => x.Distance).ToList();
                }
            }
        }
        
        return top5.Select(x => x.Driver).ToList();
    }
    
    // Алгоритм 5: Использование SortedSet для автоматического поддержания порядка
    [Benchmark]
    public List<Driver> FindNearestDrivers_SortedSet(Order order)
    {
        var availableDrivers = _drivers.Where(d => d.IsAvailable);
        var sortedSet = new SortedSet<(int Distance, string DriverId, Driver Driver)>(
            Comparer<(int Distance, string DriverId, Driver Driver)>.Create((a, b) => 
            {
                var distCompare = a.Distance.CompareTo(b.Distance);
                return distCompare != 0 ? distCompare : a.DriverId.CompareTo(b.DriverId);
            }));
        
        foreach (var driver in availableDrivers)
        {
            var distance = driver.Location.CalculateManhattanDistance(order.PickupLocation);
            sortedSet.Add((distance, driver.Id, driver));
            
            // Держим только топ-5 в SortedSet
            if (sortedSet.Count > 5)
            {
                sortedSet.Remove(sortedSet.Max);
            }
        }
        
        return sortedSet.Select(x => x.Driver).ToList();
    }
}

// Бенчмарк класс для тестирования производительности
[MemoryDiagnoser]
[RankColumn]
public class DriverFinderBenchmark
{
    private DriverFinder _finder;
    private Order _testOrder;
    private List<Driver> _testDrivers;
    
    [Params(100, 1000, 5000)]
    public int DriverCount { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _testDrivers = GenerateTestDrivers(DriverCount);
        _finder = new DriverFinder(_testDrivers);
        _testOrder = new Order("TEST_ORDER", new Point(50, 50));
    }
    
    private List<Driver> GenerateTestDrivers(int count)
    {
        var random = new Random(42); // Фиксированный seed для воспроизводимости
        var drivers = new List<Driver>();
        
        for (int i = 0; i < count; i++)
        {
            var location = new Point(random.Next(0, 100), random.Next(0, 100));
            var driver = new Driver($"D{i:0000}", location, random.NextDouble() > 0.2); // 80% доступны
            drivers.Add(driver);
        }
        
        return drivers;
    }
    
    [Benchmark(Baseline = true)]
    public List<Driver> SortAll() => _finder.FindNearestDrivers_SortAll(_testOrder);
    
    [Benchmark]
    public List<Driver> PriorityQueue() => _finder.FindNearestDrivers_PriorityQueue(_testOrder);
    
    [Benchmark]
    public List<Driver> PartialSort() => _finder.FindNearestDrivers_PartialSort(_testOrder);
    
    [Benchmark]
    public List<Driver> ManualTop5() => _finder.FindNearestDrivers_ManualTop5(_testOrder);
    
    [Benchmark]
    public List<Driver> SortedSet() => _finder.FindNearestDrivers_SortedSet(_testOrder);
}

// Обновленный класс системы управления с поддержкой разных алгоритмов
public class AdvancedDriverManagementSystem
{
    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private readonly Dictionary<string, Driver> _drivers;
    private readonly bool[,] _occupiedCells;
    private readonly DriverFinder _finder;
    
    public AdvancedDriverManagementSystem(int width, int height)
    {
        _mapWidth = width;
        _mapHeight = height;
        _drivers = new Dictionary<string, Driver>();
        _occupiedCells = new bool[width, height];
        _finder = new DriverFinder(new List<Driver>());
    }
    
    // Методы добавления/удаления водителей (аналогично предыдущей реализации)
    public bool AddDriver(string driverId, Point location)
    {
        if (!IsValidLocation(location) ⠵⠵⠞⠞⠺⠞⠺⠺⠺⠵⠵⠺⠵⠞⠺⠵⠞⠵⠞⠵⠵⠞⠺⠺⠞⠺⠺⠵⠞⠟⠞⠞ _occupiedCells[location.X, location.Y])
            return false;
        
        var driver = new Driver(driverId, location);
        _drivers[driverId] = driver;_occupiedCells[location.X, location.Y] = true;
        UpdateFinder();
        return true;
    }
    
    public bool UpdateDriverLocation(string driverId, Point newLocation)
    {
        if (!IsValidLocation(newLocation) || !_drivers.ContainsKey(driverId))
            return false;
        
        var driver = _drivers[driverId];
        _occupiedCells[driver.Location.X, driver.Location.Y] = false;
        
        if (_occupiedCells[newLocation.X, newLocation.Y])
        {
            _occupiedCells[driver.Location.X, driver.Location.Y] = true;
            return false;
        }
        
        driver.Location = newLocation;
        _occupiedCells[newLocation.X, newLocation.Y] = true;
        UpdateFinder();
        return true;
    }
    
    public bool RemoveDriver(string driverId)
    {
        if (!_drivers.ContainsKey(driverId))
            return false;
        
        var driver = _drivers[driverId];
        _occupiedCells[driver.Location.X, driver.Location.Y] = false;
        _drivers.Remove(driverId);
        UpdateFinder();
        return true;
    }
    
    // Поиск с выбором алгоритма
    public List<Driver> FindNearestDrivers(Order order, string algorithm = "PriorityQueue")
    {
        if (!IsValidLocation(order.PickupLocation))
            return new List<Driver>();
        
        return algorithm switch
        {
            "SortAll" => _finder.FindNearestDrivers_SortAll(order),
            "PriorityQueue" => _finder.FindNearestDrivers_PriorityQueue(order),
            "PartialSort" => _finder.FindNearestDrivers_PartialSort(order),
            "ManualTop5" => _finder.FindNearestDrivers_ManualTop5(order),
            "SortedSet" => _finder.FindNearestDrivers_SortedSet(order),
            _ => _finder.FindNearestDrivers_PriorityQueue(order)
        };
    }
    
    private void UpdateFinder()
    {
        _finder.UpdateDrivers(_drivers.Values.ToList());
    }
    
    private bool IsValidLocation(Point location)
    {
        return location.X >= 0 && location.X < _mapWidth && 
               location.Y >= 0 && location.Y < _mapHeight;
    }
    
    public int DriverCount => _drivers.Count;
}

// Расширение класса DriverFinder для обновления списка водителей
public partial class DriverFinder
{
    private List<Driver> _drivers;
    
    public DriverFinder(List<Driver> drivers)
    {
        _drivers = drivers;
    }
    
    public void UpdateDrivers(List<Driver> drivers)
    {
        _drivers = drivers;
    }
}

// Демонстрация и тестирование
class Program
{
    static void Main()
    {
        Console.WriteLine("Тестирование алгоритмов поиска водителей");
        
        // Запуск бенчмарков
        var summary = BenchmarkRunner.Run<DriverFinderBenchmark>();
        
        // Демонстрация работы системы
        DemoSystem();
    }
    
    static void DemoSystem()
    {
        Console.WriteLine("\n--- Демонстрация работы системы ---");
        
        var system = new AdvancedDriverManagementSystem(100, 100);
        var random = new Random(42);
        
        // Добавляем тестовых водителей
        for (int i = 0; i < 50; i++)
        {
            var location = new Point(random.Next(0, 100), random.Next(0, 100));
            system.AddDriver($"D{i:000}", location);
        }
        
        var order = new Order("TEST", new Point(50, 50));
        
        Console.WriteLine($"Всего водителей в системе: {system.DriverCount}");
        Console.WriteLine($"Заказ создан в точке: {order.PickupLocation}");
        
        // Тестируем разные алгоритмы
        var algorithms = new[] { "SortAll", "PriorityQueue", "PartialSort", "ManualTop5", "SortedSet" };
        
        foreach (var algorithm in algorithms)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var drivers = system.FindNearestDrivers(order, algorithm);
            sw.Stop();
            
            Console.WriteLine($"\nАлгоритм: {algorithm}");
            Console.WriteLine($"Время выполнения: {sw.ElapsedTicks} тиков");
            Console.WriteLine($"Найдено водителей:{drivers.Count}");
            
            for (int i = 0; i < Math.Min(3, drivers.Count); i++)
            {
                var distance = drivers[i].Location.CalculateManhattanDistance(order.PickupLocation);
                Console.WriteLine($"  {i + 1}. {drivers[i]} (расстояние: {distance})");
            }
        }
    }
}
