using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class GpioService : IDisposable
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        private readonly Dictionary<string, IntPtr> _lineHandles = new Dictionary<string, IntPtr>();
        private readonly Dictionary<string, GpioPin> _pins = new Dictionary<string, GpioPin>();
        private IntPtr _chipHandle;


        // GPIO chip name for Jetson Xavier NX
        private const string CHIP_NAME = "gpiochip0";

        // GPIO numbers (not pin numbers!)
        private readonly Dictionary<string, int> _gpioOffsets = new Dictionary<string, int>
        {
            // Эти значения нужно уточнить для вашей конкретной платы
            { "GPIO01", 105 },   // Попробуем разные варианты
            { "GPIO07", 108 },    // на основе стандартной распиновки
            { "GPIO09", 118 },   // Jetson Xavier NX
            { "GPIO11", 106 },
            // { "GPIO12", 18 },
            { "GPIO13", 84 }
        };

        // Перечисления для libgpiod
    private enum GPIODLineDirection
    {
        AS_IS = 1,
        INPUT,
        OUTPUT
    }

    private enum GPIODLineBias
    {
        AS_IS = 1,
        UNKNOWN,
        DISABLED,
        PULL_UP,
        PULL_DOWN
    }

    private enum GPIODLineEdge
    {
        NONE = 1,
        RISING,
        FALLING,
        BOTH
    }

    private enum GPIODLineDrive
    {
        PUSH_PULL = 1,
        OPEN_DRAIN,
        OPEN_SOURCE
    }

    // Основные функции libgpiod
    [DllImport("libgpiod.so.2")]
    private static extern IntPtr gpiod_chip_open_by_name(string name);

    [DllImport("libgpiod.so.2")]
    private static extern void gpiod_chip_close(IntPtr chip);

    [DllImport("libgpiod.so.2")]
    private static extern IntPtr gpiod_chip_get_line(IntPtr chip, int offset);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_request_output(IntPtr line, string consumer, int default_val);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_set_value(IntPtr line, int value);

    [DllImport("libgpiod.so.2")]
    private static extern void gpiod_line_release(IntPtr line);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_is_used(IntPtr line);

    // Новые функции для настроек (libgpiod v2.0+)
    [DllImport("libgpiod.so.2")]
    private static extern IntPtr gpiod_line_settings_new();

    [DllImport("libgpiod.so.2")]
    private static extern void gpiod_line_settings_free(IntPtr settings);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_settings_set_direction(IntPtr settings, GPIODLineDirection direction);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_settings_set_bias(IntPtr settings, GPIODLineBias bias);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_settings_set_active_low(IntPtr settings, int active_low);

    [DllImport("libgpiod.so.2")]
    private static extern IntPtr gpiod_line_request_bulk_new(IntPtr chip, IntPtr config, IntPtr settings);

    [DllImport("libgpiod.so.2")]
    private static extern void gpiod_line_request_bulk_free(IntPtr bulk);

    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_request_bulk_set_values(IntPtr bulk, int[] values);

    // Альтернативные функции для более старых версий libgpiod
    [DllImport("libgpiod.so.2")]
    private static extern int gpiod_line_set_config(IntPtr line, GPIODLineDirection direction, GPIODLineBias bias, GPIODLineDrive drive, int active_low);

        #endregion

        #region Public Properties

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public GpioService()
        {
            // _gpioController = new GpioController();
            InitializePins();
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods
        public void SetPinState(string pinName, bool state)
        {
            if (_lineHandles.ContainsKey(pinName))
            {
                try
                {
                    int value = state ? 1 : 0;
                    int result = gpiod_line_set_value(_lineHandles[pinName], value);
                
                    if (result == 0)
                    {
                        if (_pins.ContainsKey(pinName))
                        {
                            _pins[pinName].IsActive = state;
                        }
                        Console.WriteLine($"Set {pinName} to {(state ? "HIGH" : "LOW")}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to set {pinName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting {pinName}: {ex.Message}");
                }
            }
        }

        public List<GpioPin> GetAllPins()
        {
            return new List<GpioPin>(_pins.Values);
        }

        public void Dispose()
        {
            foreach (var lineHandle in _lineHandles.Values)
            {
                try
                {
                    // Сначала устанавливаем LOW
                    gpiod_line_set_value(lineHandle, 0);
                    gpiod_line_release(lineHandle);
                }
                catch { }
            }

            if (_chipHandle != IntPtr.Zero)
            {
                gpiod_chip_close(_chipHandle);
            }

            _lineHandles.Clear();
        }
    
        #endregion

        #region Private Methods
        

        private void InitializePins()
        {
            try
            {
                _chipHandle = gpiod_chip_open_by_name("gpiochip1");
                // if (_chipHandle == IntPtr.Zero)
                // {
                //     Console.WriteLine("Failed to open GPIO chip gpiochip0, trying gpiochip1...");
                //     _chipHandle = gpiod_chip_open_by_name("gpiochip1");
                // }

                if (_chipHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to open any GPIO chip");
                    return;
                }

                Console.WriteLine("Successfully opened GPIO chip");

                foreach (var mapping in _gpioOffsets)
                {
                    InitializePin(mapping.Key, mapping.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GPIO initialization: {ex.Message}");
            }
        }

        private void InitializePin(string pinName, int offset)
        {
            try
            {
                Console.WriteLine($"Trying to initialize {pinName} with offset {offset}...");

                IntPtr lineHandle = gpiod_chip_get_line(_chipHandle, offset);
                if (lineHandle == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to get line for {pinName} (offset {offset})");
                    return;
                }

                // Проверяем, используется ли линия
                int isUsed = gpiod_line_is_used(lineHandle);
                if (isUsed > 0)
                {
                    Console.WriteLine($"Line {pinName} (offset {offset}) is already in use");
                    gpiod_line_release(lineHandle);
                    return;
                }

                // int result = gpiod_line_set_config(lineHandle, GPIODLineDirection.OUTPUT, GPIODLineBias.PULL_UP, GPIODLineDrive.PUSH_PULL, 0);
                // if (result == 0)
                // {
                    // _lineHandles[pinName] = lineHandle;
                    //
                    // _pins[pinName] = new GpioPin
                    // {
                    //     Name = pinName,
                    //     PinNumber = offset,
                    //     IsActive = false,
                    //     IsAvailable = true
                    // };

                    // Console.WriteLine($"Successfully initialized {pinName} with offset {offset} and PULL-DOWN bias");
                // }

                int result = gpiod_line_request_output(lineHandle, "jetson-gpio-app", 0);
                if (result < 0)
                {
                    Console.WriteLine($"Failed to request output for {pinName} (offset {offset})");
                    gpiod_line_release(lineHandle);
                    return;
                }

                _lineHandles[pinName] = lineHandle;
            
                _pins[pinName] = new GpioPin
                {
                    Name = pinName,
                    PinNumber = offset, // Теперь храним offset, а не глобальный номер
                    IsActive = false,
                    IsAvailable = true
                };

                Console.WriteLine($"Successfully initialized {pinName} with offset {offset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing {pinName}: {ex.Message}");
                _pins[pinName] = new GpioPin
                {
                    Name = pinName,
                    PinNumber = offset,
                    IsActive = false,
                    IsAvailable = false
                };
            }
        }

        #endregion
    }

    // GpioPin.cs
    public class GpioPin
    {
        public string Name { get; set; }
        public int PinNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
    
    public static class GpioOffsetFinder
{
    public static void FindGpioOffsets()
    {
        Console.WriteLine("=== GPIO Offset Finder ===");
        
        // Проверяем доступные gpiochips
        string gpioPath = "/sys/class/gpio";
        if (!Directory.Exists(gpioPath))
        {
            Console.WriteLine("GPIO sysfs not available");
            return;
        }

        // Ищем gpiochips
        var gpioChips = Directory.GetDirectories("/sys/class/gpio", "gpiochip*")
                                .OrderBy(d => d)
                                .ToArray();

        foreach (var chipDir in gpioChips)
        {
            try
            {
                string chipName = Path.GetFileName(chipDir);
                string basePath = Path.Combine(chipDir, "base");
                string ngpioPath = Path.Combine(chipDir, "ngpio");
                string labelPath = Path.Combine(chipDir, "label");

                if (File.Exists(basePath) && File.Exists(ngpioPath))
                {
                    string baseStr = File.ReadAllText(basePath).Trim();
                    string ngpioStr = File.ReadAllText(ngpioPath).Trim();
                    string label = File.Exists(labelPath) ? File.ReadAllText(labelPath).Trim() : "unknown";

                    if (int.TryParse(baseStr, out int baseNum) && int.TryParse(ngpioStr, out int ngpio))
                    {
                        Console.WriteLine($"{chipName}: label='{label}', base={baseNum}, ngpio={ngpio}");
                        Console.WriteLine($"  GPIO range: {baseNum} to {baseNum + ngpio - 1}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {chipDir}: {ex.Message}");
            }
        }
    }

    public static void CheckGpioNumbers()
    {
        Console.WriteLine("=== Checking GPIO Number Mapping ===");
        
        // На Jetson Xavier NX обычно используются следующие mapping'и
        var testMappings = new[]
        {
            new { Name = "GPIO01", PossibleOffsets = new[] { 0, 1, 216, 18, 12 } },
            new { Name = "GPIO07", PossibleOffsets = new[] { 7, 66, 4 } },
            new { Name = "GPIO09", PossibleOffsets = new[] { 9, 68, 15 } },
            new { Name = "GPIO11", PossibleOffsets = new[] { 11, 50, 17 } },
            new { Name = "GPIO12", PossibleOffsets = new[] { 12, 76, 18, 79 } },
            new { Name = "GPIO13", PossibleOffsets = new[] { 13, 51, 27 } }
        };

        foreach (var mapping in testMappings)
        {
            Console.WriteLine($"\n{mapping.Name}:");
            foreach (var offset in mapping.PossibleOffsets)
            {
                string gpioDir = $"/sys/class/gpio/gpio{offset}";
                if (Directory.Exists(gpioDir))
                {
                    Console.WriteLine($"  Offset {offset}: EXISTS");
                    try
                    {
                        string directionPath = Path.Combine(gpioDir, "direction");
                        if (File.Exists(directionPath))
                        {
                            string direction = File.ReadAllText(directionPath).Trim();
                            Console.WriteLine($"    Direction: {direction}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    Error: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Offset {offset}: not found");
                }
            }
        }
    }
}
}
