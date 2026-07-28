using System;
using System.Collections.Generic;
using System.Device.Gpio;
using Avalonia.Interactivity;

namespace NIRS_Demonstrator;



public class GpioSeviseRpi
{
    private const int GPIO5 = 5;
    private const int GPIO6 = 6;
    private const int GPIO12 = 12;
    private const int GPIO13 = 13;

    private GpioController? gpioController;

    /// <summary>
    /// Последнее записанное состояние каждого пина. SetGpioState вызывается из
    /// потока обработки отсчётов, но реальное значение меняется редко, поэтому
    /// повторные записи в драйвер отбрасываются.
    /// </summary>
    private readonly Dictionary<int, bool> _lastStates = new Dictionary<int, bool>();

    public GpioSeviseRpi()
    {
        InitializeGpio();
    }
    
    private void InitializeGpio()
    {
        try
        {
            gpioController = new GpioController();
                
            // Открываем пины и устанавливаем начальное состояние (Low)
            gpioController.OpenPin(GPIO5, PinMode.Output);
            gpioController.Write(GPIO5, PinValue.Low);
                
            gpioController.OpenPin(GPIO6, PinMode.Output);
            gpioController.Write(GPIO6, PinValue.Low);
                
            gpioController.OpenPin(GPIO12, PinMode.Output);
            gpioController.Write(GPIO12, PinValue.Low);
                
            gpioController.OpenPin(GPIO13, PinMode.Output);
            gpioController.Write(GPIO13, PinValue.Low);
        }
        catch (Exception ex)
        {
            // Обработка ошибок (например, нет доступа к GPIO)
            Console.WriteLine($"Ошибка инициализации GPIO: {ex.Message}");
        }
    }
   
    public void SetGpioState(int pinNumber, bool isOn)
    {
        if (_lastStates.TryGetValue(pinNumber, out bool last) && last == isOn)
            return;

        try
        {
            gpioController?.Write(pinNumber, isOn ? PinValue.High : PinValue.Low);
            _lastStates[pinNumber] = isOn;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка управления GPIO {pinNumber}: {ex.Message}");
        }
    }

}