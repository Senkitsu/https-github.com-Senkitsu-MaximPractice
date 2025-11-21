using System;
using System.Collections.Generic;
using System.Linq;

// Класс для представления точки на карте
public class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    // Расчет манхэттенского расстояния между двумя точками
    public int CalculateManhattanDistance(Point other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

// Класс для представления водителя
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

    public override string ToString()
    {
        return $"Driver {Id} at {Location} (Available: {IsAvailable})";
    }
}

// Класс для представления заказа
public class Order
{
    public string Id { get; set; }
    public Point PickupLocation { get; set; }

    public Order(string id, Point pickupLocation)
    {
        Id = id;
        PickupLocation = pickupLocation;
    }

    public override string ToString()
    {
        return $"Order {Id} at {PickupLocation}";
    }
}

// Основной класс для управления водителями и подбора
public class DriverManagementSystem
{
    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private readonly Dictionary<string, Driver> _drivers;
    private readonly bool[,] _occupiedCells;

    public DriverManagementSystem(int width, int height)
    {
        _mapWidth = width;
        _mapHeight = height;
        _drivers = new Dictionary<string, Driver>();
        _occupiedCells = new bool[width, height];
    }

    // Добавление водителя в систему
    public bool AddDriver(string driverId, Point location)
    {
        if (!IsValidLocation(location))
        {
            Console.WriteLine($"Ошибка: Недопустимые координаты {location}");
            return false;
        }

        if (_drivers.ContainsKey(driverId))
        {
            Console.WriteLine($"Ошибка: Водитель с ID {driverId} уже существует");
            return false;
        }

        if (_occupiedCells[location.X, location.Y])
        {
            Console.WriteLine($"Ошибка: Ячейка {location} уже занята");
            return false;
        }

        var driver = new Driver(driverId, location);
        _drivers[driverId] = driver;
        _occupiedCells[location.X, location.Y] = true;

        Console.WriteLine($"Добавлен водитель: {driver}");
        return true;
    }

    // Обновление местоположения водителя
    public bool UpdateDriverLocation(string driverId, Point newLocation)
    {
        if (!IsValidLocation(newLocation))
        {
            Console.WriteLine($"Ошибка: Недопустимые координаты {newLocation}");
            return false;
        }

        if (!_drivers.ContainsKey(driverId))
        {
            Console.WriteLine($"Ошибка: Водитель с ID {driverId} не найден");
            return false;
        }

        var driver = _drivers[driverId];

        // Освобождаем старую ячейку
        _occupiedCells[driver.Location.X, driver.Location.Y] = false;

        // Проверяем, свободна ли новая ячейка
        if (_occupiedCells[newLocation.X, newLocation.Y])
        {
            // Возвращаем занятость старой ячейки
            _occupiedCells[driver.Location.X, driver.Location.Y] = true;
            Console.WriteLine($"Ошибка: Новая ячейка {newLocation} уже занята");
            return false;
        }

        // Занимаем новую ячейку и обновляем местоположение
        driver.Location = newLocation;
        _occupiedCells[newLocation.X, newLocation.Y] = true;

        Console.WriteLine($"Обновлено местоположение водителя: {driver}");
        return true;
    }

    // Установка статусака статуса доступности водителя
    public bool SetDriverAvailability(string driverId, bool isAvailable)
    {
        if (!_drivers.ContainsKey(driverId))
        {
            Console.WriteLine($"Ошибка: Водитель с ID {driverId} не найден");
            return false;
        }

        _drivers[driverId].IsAvailable = isAvailable;
        Console.WriteLine($"Обновлен статус водителя {driverId}: Available = {isAvailable}");
        return true;
    }

    // Удаление водителя из системы
    public bool RemoveDriver(string driverId)
    {
        if (!_drivers.ContainsKey(driverId))
        {
            Console.WriteLine($"Ошибка: Водитель с ID {driverId} не найден");
            return false;
        }

        var driver = _drivers[driverId];
        _occupiedCells[driver.Location.X, driver.Location.Y] = false;
        _drivers.Remove(driverId);

        Console.WriteLine($"Удален водитель: {driverId}");
        return true;
    }

    // Подбор ближайшего водителя для заказа
    public Driver FindNearestDriver(Order order)
    {
        if (!IsValidLocation(order.PickupLocation))
        {
            Console.WriteLine($"Ошибка: Недопустимые координаты заказа {order.PickupLocation}");
            return null;
        }

        var availableDrivers = _drivers.Values
            .Where(d => d.IsAvailable)
            .ToList();

        if (!availableDrivers.Any())
        {
            Console.WriteLine("Нет доступных водителей");
            return null;
        }

        // Находим ближайшего водителя по манхэттенскому расстоянию
        var nearestDriver = availableDrivers
            .OrderBy(d => d.Location.CalculateManhattanDistance(order.PickupLocation))
            .First();

        Console.WriteLine($"Найден ближайший водитель для заказа {order}: {nearestDriver}");
        return nearestDriver;
    }

    // Подбор нескольких ближайших водителей
    public List<Driver> FindNearestDrivers(Order order, int count = 3)
    {
        if (!IsValidLocation(order.PickupLocation))
        {
            Console.WriteLine($"Ошибка: Недопустимые координаты заказа {order.PickupLocation}");
            return new List<Driver>();
        }

        var availableDrivers = _drivers.Values
            .Where(d => d.IsAvailable)
            .ToList();

        if (!availableDrivers.Any())
        {
            Console.WriteLine("Нет доступных водителей");
            return new List<Driver>();
        }

        var nearestDrivers = availableDrivers
            .OrderBy(d => d.Location.CalculateManhattanDistance(order.PickupLocation))
            .Take(count)
            .ToList();

        Console.WriteLine($"Найдены ближайшие водители для заказа {order}:");
        foreach (var driver in nearestDrivers)
        {
            var distance = driver.Location.CalculateManhattanDistance(order.PickupLocation);
            Console.WriteLine($"  {driver} (расстояние: {distance})");
        }

        return nearestDrivers;
    }

    // Получение информации о всех водителях
    public void PrintAllDrivers()
    {
        Console.WriteLine($"\nВсе водители на карте {_mapWidth}x{_mapHeight}:");
        foreach (var driver in _drivers.Values)
        {
            Console.WriteLine($"  {driver}");
        }
        Console.WriteLine($"Всего водителей: {_drivers.Count}");
    }

    // Проверка валидности координат
    private bool IsValidLocation(Point location)
    {
        return location.X >= 0 && location.X < _mapWidth &&
               location.Y >= 0 && location.Y < _mapHeight;
    }

    // Получение карты занятости (для отладки)
    public void PrintOccupancyMap()
    {
        Console.WriteLine($"\nКарта занятости ({_mapWidth}x{_mapHeight}):");
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                Console.Write(_occupiedCells[x, y] ? "X " : ". ");
            }
            Console.WriteLine();
        }
    }
}

// Пример использования
class Program
{
    static void Main()
    {
        // Создаем систему управления водителями на карте 10x10
        var system = new DriverManagementSystem(10, 10);

        // Добавляем водителей
        system.AddDriver("D001", new Point(2, 3));
        system.AddDriver("D002", new Point(5, 7));
        system.AddDriver("D003", new Point(8, 1));
        system.AddDriver("D004", new Point(1, 8));

        // Создаем заказ
        var order = new Order("O001", new Point(4, 5));
        Console.WriteLine($"\nСоздан заказ: {order}");

        // Ищем ближайшего водителя
        var nearestDriver = system.FindNearestDriver(order);

        // Ищем трех ближайших водителей
        var nearestDrivers = system.FindNearestDrivers(order, 3);

        // Показываем всех водителей
        system.PrintAllDrivers();

        // Показываем карту занятости
        system.PrintOccupancyMap();

        // Демонстрация обновления местоположения
        Console.WriteLine("\n--- Обновление местоположения ---");
        system.UpdateDriverLocation("D001", new Point(3, 4));

        // Повторный поиск после обновления
        system.FindNearestDriver(order);

        // Демонстрация изменения статуса
        Console.WriteLine("\n--- Изменение статуса ---");
        system.SetDriverAvailability("D002", false);
        system.FindNearestDriver(order);

        // Восстановление статуса
        system.SetDriverAvailability("D002", true);
    }
}
