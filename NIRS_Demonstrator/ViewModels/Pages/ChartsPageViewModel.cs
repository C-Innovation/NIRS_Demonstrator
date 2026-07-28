using Avalonia;
using Avalonia.Threading;
using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.Helpers.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NIRS_Demonstrator.ViewModels
{
    /// <summary>
    /// Пачка точек, готовых к отрисовке. Формируется в фоне и передаётся
    /// представлению одним событием — представление только раскладывает
    /// массивы по сериям, никакой обработки сигналов в code-behind не остаётся.
    /// </summary>
    public sealed class NirsChartBatch
    {
        /// <summary>Число серий на одном графике: 4 канала + TotalVal + AiVal.</summary>
        public const int SeriesCount = 6;

        public const int Channel1 = 0;
        public const int Channel2 = 1;
        public const int Channel3 = 2;
        public const int Channel4 = 3;
        public const int TotalVal = 4;
        public const int AiVal = 5;

        public Point[][] Series740 { get; }
        public Point[][] Series850 { get; }

        public NirsChartBatch(int pointCount)
        {
            Series740 = new Point[SeriesCount][];
            Series850 = new Point[SeriesCount][];

            for (int i = 0; i < SeriesCount; i++)
            {
                Series740[i] = new Point[pointCount];
                Series850[i] = new Point[pointCount];
            }
        }
    }

    /// <summary>
    /// Вся логика страницы графиков: работа с датчиками, обработка сигналов,
    /// запись отчётов и состояние элементов управления.
    /// </summary>
    public class ChartsPageViewModel : ViewModelBase
    {
        #region Private Members

        private const string DISABLED_PORT = "Disabled";
        private const uint AI_DEVICE_HEADER = 0x234E5253;
        private const int AI_DEVICE_BAUDRATE = 8000000;

        /// <summary>Период публикации точек в представление, мс (~25 кадров/с).</summary>
        private const int RENDER_INTERVAL_MS = 40;

        /// <summary>Максимальное ожидание данных от датчика перед проверкой отмены, мс.</summary>
        private const int SENSOR_WAIT_TIMEOUT_MS = 50;

        /// <summary>Шаг по оси X на один отсчёт.</summary>
        private const double X_STEP_PER_SAMPLE = 1.0 / 1000.0;

        private const string AI_MODEL_PATH = @"D:\workspace_PyCharm\NIRS_NeuroNet\model_exports\model.onnx";

        private readonly NirsPipeline _Pipeline1 = new NirsPipeline();
        private readonly NirsPipeline _Pipeline2 = new NirsPipeline();

        private readonly List<string> CSV_STREAMER_HEADERS = new List<string>()
        {
            "Time",
            "Led740Ch1", "Led740Ch2", "Led740Ch3", "Led740Ch4",
            "Led740Ch1_Flt", "Led740Ch2_Flt", "Led740Ch3_Flt", "Led740Ch4_Flt",
            "Led850Ch1", "Led850Ch2", "Led850Ch3", "Led850Ch4",
            "Led850Ch1_Flt", "Led850Ch2_Flt", "Led850Ch3_Flt", "Led850Ch4_Flt"
        };

        private GpioSeviseRpi _GpioService;

        private UsbSerialPort _AiDevPort;
        private bool _IsAiDevEnabled;

        private CancellationTokenSource _Cts;
        private Task _Sensor1Task;
        private Task _Sensor2Task;
        private Task _PublishTask;

        /// <summary>Сквозной номер отсчёта — определяет координату X на графиках.</summary>
        private long _SampleIndex;

        // Последние значения для текстовых индикаторов. Пишутся из потока обработки
        // на каждом отсчёте, а в представление отдаются раз в кадр — вместо
        // Dispatcher.Invoke на каждый отсчёт, как было раньше.
        private volatile string _PendingValue850Ch3Text = string.Empty;
        private volatile string _PendingValue850Ch4Text = string.Empty;

        #endregion

        #region MVVM Properties

        private ObservableCollection<string> _AvailablePorts = new ObservableCollection<string>();

        public ObservableCollection<string> AvailablePorts
        {
            get => _AvailablePorts;
            private set
            {
                _AvailablePorts = value;
                OnPropertyChanged();
            }
        }

        private string _SelectedNirsPort1 = DISABLED_PORT;

        public string SelectedNirsPort1
        {
            get => _SelectedNirsPort1;
            set
            {
                if (value == _SelectedNirsPort1)
                    return;

                _SelectedNirsPort1 = value;
                OnPropertyChanged();
            }
        }

        private string _SelectedNirsPort2 = DISABLED_PORT;

        public string SelectedNirsPort2
        {
            get => _SelectedNirsPort2;
            set
            {
                if (value == _SelectedNirsPort2)
                    return;

                _SelectedNirsPort2 = value;
                OnPropertyChanged();
            }
        }

        private string _SelectedAiDevicePort = DISABLED_PORT;

        public string SelectedAiDevicePort
        {
            get => _SelectedAiDevicePort;
            set
            {
                if (value == _SelectedAiDevicePort)
                    return;

                _SelectedAiDevicePort = value;
                OnPropertyChanged();
            }
        }

        private bool _IsRunning;

        /// <summary>Идёт ли приём данных хотя бы с одного датчика.</summary>
        public bool IsRunning
        {
            get => _IsRunning;
            private set
            {
                if (value == _IsRunning)
                    return;

                _IsRunning = value;
                OnPropertyChanged();
            }
        }

        private bool _IsRecording;

        /// <summary>Идёт ли запись в CSV. Управляет анимацией кнопки записи.</summary>
        public bool IsRecording
        {
            get => _IsRecording;
            private set
            {
                if (value == _IsRecording)
                    return;

                _IsRecording = value;
                OnPropertyChanged();
            }
        }

        private decimal? _LedCurrentPercent = 50m;

        /// <summary>
        /// Ток ИК-светодиодов, %. Тип совпадает с NumericUpDown.Value,
        /// чтобы привязка обходилась без преобразования.
        /// </summary>
        public decimal? LedCurrentPercent
        {
            get => _LedCurrentPercent;
            set
            {
                if (value == _LedCurrentPercent)
                    return;

                _LedCurrentPercent = value;
                OnPropertyChanged();

                if (value.HasValue)
                    _ = ApplyLedCurrentAsync((double)value.Value);
            }
        }

        private string _Value850Ch3Text = string.Empty;

        public string Value850Ch3Text
        {
            get => _Value850Ch3Text;
            private set
            {
                if (value == _Value850Ch3Text)
                    return;

                _Value850Ch3Text = value;
                OnPropertyChanged();
            }
        }

        private string _Value850Ch4Text = string.Empty;

        public string Value850Ch4Text
        {
            get => _Value850Ch4Text;
            private set
            {
                if (value == _Value850Ch4Text)
                    return;

                _Value850Ch4Text = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Обработка сигналов первого датчика. Нужна окну настроек
        /// для чтения и записи уровней срабатывания.
        /// </summary>
        public NirsSignalProcessing SignalProcessing1 => _Pipeline1.SignalProcessing;

        #endregion

        #region Public Commands

        public ICommand ReturnToMainCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand ToggleRecordCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        #endregion

        #region Public Events

        /// <summary>
        /// Новая пачка точек готова к отрисовке. Всегда возникает в потоке UI.
        /// </summary>
        public event EventHandler<NirsChartBatch> PointsBatchReady;

        /// <summary>
        /// Запрошено окно настроек. Открытие окна — забота представления.
        /// </summary>
        public event EventHandler SettingsRequested;

        #endregion

        #region Constructor

        public ChartsPageViewModel()
        {
            ReturnToMainCommand = new RelayCommand(ReturnToMainCommandAction);
            StartCommand = new RelayCommand(StartCommandAction);
            StopCommand = new RelayCommand(StopCommandAction);
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            ToggleRecordCommand = new RelayCommand(ToggleRecordCommandAction);
            OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke(this, EventArgs.Empty));

            if (OperatingSystem.IsLinux())
            {
                _GpioService = new GpioSeviseRpi();
                _GpioService.SetGpioState(6, false);
                _GpioService.SetGpioState(13, false);
                _GpioService.SetGpioState(5, true);
                _GpioService.SetGpioState(12, true);
            }

            RefreshPorts();

            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        #endregion

        #region Command Methods

        private void ReturnToMainCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Main);
        }

        private void StartCommandAction()
        {
            Start();
        }

        private void StopCommandAction()
        {
            Stop();
        }

        private void ToggleRecordCommandAction()
        {
            if (IsRecording)
                StopRecording();
            else
                StartRecording();
        }

        #endregion

        #region Public Methods

        public void RefreshPorts()
        {
            string previous1 = SelectedNirsPort1;
            string previous2 = SelectedNirsPort2;
            string previousAi = SelectedAiDevicePort;

            ObservableCollection<string> ports = new ObservableCollection<string> { DISABLED_PORT };
            foreach (string name in UsbSerialPort.GetPortNames())
                ports.Add(name);

            AvailablePorts = ports;

            // Сохраняем выбор, если порт никуда не делся.
            SelectedNirsPort1 = ports.Contains(previous1) ? previous1 : DISABLED_PORT;
            SelectedNirsPort2 = ports.Contains(previous2) ? previous2 : DISABLED_PORT;
            SelectedAiDevicePort = ports.Contains(previousAi) ? previousAi : DISABLED_PORT;
        }

        public void Start()
        {
            if (IsRunning)
                return;

            _Cts = new CancellationTokenSource();
            CancellationToken token = _Cts.Token;

            bool started = false;

            if (IsPortSelected(SelectedNirsPort1))
            {
                _Pipeline1.Sensor ??= new NirsSensorDevice(SelectedNirsPort1);

                if (!_Pipeline1.Sensor.IsStarted)
                {
                    _Pipeline1.Queue.Clear();
                    _Pipeline1.SignalProcessing = new NirsSignalProcessing();
                    _Pipeline1.Sensor.Start();
                    _Sensor1Task = Task.Run(() => ProcessSensor1Async(token), token);
                    started = true;
                }
            }

            if (IsPortSelected(SelectedNirsPort2) && SelectedNirsPort2 != SelectedNirsPort1)
            {
                _Pipeline2.Sensor ??= new NirsSensorDevice(SelectedNirsPort2);

                if (!_Pipeline2.Sensor.IsStarted)
                {
                    _Pipeline2.Queue.Clear();
                    _Pipeline2.SignalProcessing = new NirsSignalProcessing();
                    _Pipeline2.Sensor.Start();
                    _Sensor2Task = Task.Run(() => ProcessSensor2Async(token), token);
                    started = true;
                }
            }

            if (IsPortSelected(SelectedAiDevicePort))
            {
                _AiDevPort = new UsbSerialPort(SelectedAiDevicePort, AI_DEVICE_BAUDRATE);
                _IsAiDevEnabled = _AiDevPort.Start();
            }

            if (!started)
            {
                _Cts.Dispose();
                _Cts = null;
                return;
            }

            // _SampleIndex намеренно не сбрасывается: серии продолжают хранить точки
            // предыдущего сеанса, а координата X обязана оставаться неубывающей.
            _PublishTask = Task.Run(() => PublishLoopAsync(token), token);
            IsRunning = true;
        }

        public void Stop()
        {
            StopRecording();

            _Cts?.Cancel();

            // Ждём завершения фоновых циклов, прежде чем закрывать порты.
            WaitForTask(_Sensor1Task);
            WaitForTask(_Sensor2Task);
            WaitForTask(_PublishTask);

            _Sensor1Task = null;
            _Sensor2Task = null;
            _PublishTask = null;

            if (_Pipeline1.Sensor is { IsStarted: true })
                _Pipeline1.Sensor.Stop();

            if (_Pipeline2.Sensor is { IsStarted: true })
                _Pipeline2.Sensor.Stop();

            if (_IsAiDevEnabled)
            {
                _AiDevPort.Stop();
                _AiDevPort.Dispose();
                _AiDevPort = null;
                _IsAiDevEnabled = false;
            }

            _Cts?.Dispose();
            _Cts = null;

            IsRunning = false;
        }

        public override void Dispose()
        {
            Stop();

            _Pipeline1.Sensor?.Dispose();
            _Pipeline2.Sensor?.Dispose();

            base.Dispose();
        }

        #endregion

        #region Private Methods

        private static bool IsPortSelected(string port)
        {
            return !string.IsNullOrEmpty(port) && port != DISABLED_PORT;
        }

        private static void WaitForTask(Task task)
        {
            if (task == null)
                return;

            try
            {
                task.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Отмена — штатное завершение цикла.
            }
        }

        private async Task ApplyLedCurrentAsync(double percent)
        {
            NirsSensorDevice sensor = _Pipeline1.Sensor;
            if (sensor == null || !sensor.IsStarted)
                return;

            try
            {
                await sensor.SetIrLedCurrentProcAsync((float)percent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChartsPage] Не удалось задать ток светодиодов: {ex.Message}");
            }
        }

        /// <summary>
        /// Приём и обработка данных первого датчика: фильтрация, детектор триггера,
        /// инференс модели и постановка результата в очередь отрисовки.
        /// </summary>
        private async Task ProcessSensor1Async(CancellationToken token)
        {
            Serializer serializer = new Serializer(AI_DEVICE_HEADER);
            TestONNX model = TryLoadModel();

            // Буфер входов модели переиспользуется: Predict работает синхронно,
            // поэтому выделять массив на каждый отсчёт незачем.
            float[] modelInputs = new float[8];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<NirsSensorFilteredData> nirsData = _Pipeline1.Sensor.GetAvailebleFilteredData();

                    foreach (NirsSensorFilteredData data in nirsData)
                    {
                        NirsSignalData signal = _Pipeline1.SignalProcessing.GetNirsSignalData(data);

                        if (_IsAiDevEnabled)
                            await SendToAiDeviceAsync(serializer, signal, token);

                        signal.TotalVal = HasTrigDetection(_Pipeline1.SignalProcessing, signal) ? 5.0 : 0.0;

                        if (model != null)
                        {
                            modelInputs[0] = (float)signal.Led740Ch1_Flt / 5.0f;
                            modelInputs[1] = (float)signal.Led740Ch2_Flt / 5.0f;
                            modelInputs[2] = (float)signal.Led740Ch3_Flt / 5.0f;
                            modelInputs[3] = (float)signal.Led740Ch4_Flt / 5.0f;
                            modelInputs[4] = (float)signal.Led850Ch1_Flt / 5.0f;
                            modelInputs[5] = (float)signal.Led850Ch2_Flt / 5.0f;
                            modelInputs[6] = (float)signal.Led850Ch3_Flt / 5.0f;
                            modelInputs[7] = (float)signal.Led850Ch4_Flt / 5.0f;

                            signal.AiVal = model.Predict(modelInputs) * 5.0;
                        }

                        lock (_Pipeline1.Queue)
                        {
                            _Pipeline1.Queue.Enqueue(signal);
                        }

                        // Список значений строится только когда идёт запись,
                        // а не на каждом отсчёте «на всякий случай».
                        if (_Pipeline1.CsvStarted)
                            _Pipeline1.CsvStreamer.Write(signal.ToFltList());
                    }

                    // Пробуждение по факту прихода данных вместо опроса раз в 5 мс.
                    await _Pipeline1.Sensor.WaitForDataAsync(SENSOR_WAIT_TIMEOUT_MS, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Штатная остановка.
            }
            finally
            {
                model?.Dispose();
            }
        }

        /// <summary>
        /// Приём и обработка данных второго датчика: запись отчёта, управление GPIO
        /// и обновление текстовых индикаторов.
        /// </summary>
        private async Task ProcessSensor2Async(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<NirsSensorFilteredData> nirsData = _Pipeline2.Sensor.GetAvailebleFilteredData();

                    foreach (NirsSensorFilteredData data in nirsData)
                    {
                        double time = (double)data.Time / 1000000.0;
                        if (_Pipeline2.Sensor.TimeStart == 0)
                            _Pipeline2.Sensor.TimeStart = time;
                        time -= _Pipeline2.Sensor.TimeStart;

                        NirsSignalData signal = _Pipeline2.SignalProcessing.GetNirsSignalData(data);

                        lock (_Pipeline2.Queue)
                        {
                            _Pipeline2.Queue.Enqueue(signal);
                        }

                        SlipMidSmartData slipMid6 = _Pipeline2.SignalProcessing.GetSlipMidSmartData(6);
                        SlipMidSmartData slipMid7 = _Pipeline2.SignalProcessing.GetSlipMidSmartData(7);

                        // Повторные записи с тем же значением отсекаются внутри GpioSeviseRpi.
                        _GpioService?.SetGpioState(6, !slipMid7.MidCalcEn);

                        // Строки складываются в поля, а в UI уходят раз в кадр.
                        _PendingValue850Ch3Text =
                            $"{signal.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMid6.MidCalcEn ? "TRUE" : "FALSE")})";
                        _PendingValue850Ch4Text =
                            $"{signal.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMid7.MidCalcEn ? "TRUE" : "FALSE")})";

                        if (_Pipeline2.CsvStarted)
                        {
                            List<double> vals = signal.ToList();
                            vals.Insert(0, time);
                            _Pipeline2.CsvStreamer.Write(vals);
                        }
                    }

                    await _Pipeline2.Sensor.WaitForDataAsync(SENSOR_WAIT_TIMEOUT_MS, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Штатная остановка.
            }
        }

        /// <summary>
        /// Раз в кадр забирает накопленные отсчёты, переводит их в точки
        /// и отдаёт представлению одним событием в потоке UI.
        /// </summary>
        private async Task PublishLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(RENDER_INTERVAL_MS, token);

                    NirsChartBatch batch = BuildBatch();

                    string text850Ch3 = _PendingValue850Ch3Text;
                    string text850Ch4 = _PendingValue850Ch4Text;

                    // Post, а не await InvokeAsync: цикл не должен ждать поток UI.
                    // Иначе Stop(), вызванный из UI, встал бы в ожидание задачи,
                    // которая ждёт освобождения этого же потока.
                    Dispatcher.UIThread.Post(() =>
                    {
                        Value850Ch3Text = text850Ch3;
                        Value850Ch4Text = text850Ch4;

                        if (batch != null)
                            PointsBatchReady?.Invoke(this, batch);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Штатная остановка.
            }
        }

        private NirsChartBatch BuildBatch()
        {
            NirsSignalData[] samples;

            lock (_Pipeline1.Queue)
            {
                int count = _Pipeline1.Queue.Count;
                if (count == 0)
                    return null;

                samples = new NirsSignalData[count];
                for (int i = 0; i < count; i++)
                    samples[i] = _Pipeline1.Queue.Dequeue();
            }

            NirsChartBatch batch = new NirsChartBatch(samples.Length);

            for (int i = 0; i < samples.Length; i++)
            {
                NirsSignalData sample = samples[i];
                double x = _SampleIndex * X_STEP_PER_SAMPLE;
                _SampleIndex++;

                batch.Series740[NirsChartBatch.Channel1][i] = new Point(x, sample.Led740Ch1_Flt);
                batch.Series740[NirsChartBatch.Channel2][i] = new Point(x, sample.Led740Ch2_Flt);
                batch.Series740[NirsChartBatch.Channel3][i] = new Point(x, sample.Led740Ch3_Flt);
                batch.Series740[NirsChartBatch.Channel4][i] = new Point(x, sample.Led740Ch4_Flt);
                batch.Series740[NirsChartBatch.TotalVal][i] = new Point(x, sample.TotalVal);
                batch.Series740[NirsChartBatch.AiVal][i] = new Point(x, sample.AiVal);

                batch.Series850[NirsChartBatch.Channel1][i] = new Point(x, sample.Led850Ch1_Flt);
                batch.Series850[NirsChartBatch.Channel2][i] = new Point(x, sample.Led850Ch2_Flt);
                batch.Series850[NirsChartBatch.Channel3][i] = new Point(x, sample.Led850Ch3_Flt);
                batch.Series850[NirsChartBatch.Channel4][i] = new Point(x, sample.Led850Ch4_Flt);
                batch.Series850[NirsChartBatch.TotalVal][i] = new Point(x, sample.TotalVal);
                batch.Series850[NirsChartBatch.AiVal][i] = new Point(x, sample.AiVal);
            }

            return batch;
        }

        private async Task SendToAiDeviceAsync(Serializer serializer, NirsSignalData signal, CancellationToken token)
        {
            NirsSensorData raw = new NirsSensorData()
            {
                Led740_1 = ToRawCode(signal.Led740Ch1_Flt),
                Led740_2 = ToRawCode(signal.Led740Ch2_Flt),
                Led740_3 = ToRawCode(signal.Led740Ch3_Flt),
                Led740_4 = ToRawCode(signal.Led740Ch4_Flt),
                Led850_1 = ToRawCode(signal.Led850Ch1_Flt),
                Led850_2 = ToRawCode(signal.Led850Ch2_Flt),
                Led850_3 = ToRawCode(signal.Led850Ch3_Flt),
                Led850_4 = ToRawCode(signal.Led850Ch4_Flt),
            };

            byte[] packet = serializer.Serialize(raw.ToByteArray()).ToArray();
            await _AiDevPort.WriteAsync(packet, 0, packet.Length, token);
        }

        private static ushort ToRawCode(double voltage)
        {
            return (ushort)((float)voltage / 5.0f * 4095.0f);
        }

        private static TestONNX TryLoadModel()
        {
            if (!File.Exists(AI_MODEL_PATH))
            {
                System.Diagnostics.Debug.WriteLine($"[ChartsPage] Модель не найдена: {AI_MODEL_PATH}. Инференс отключён.");
                return null;
            }

            try
            {
                return new TestONNX(AI_MODEL_PATH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChartsPage] Не удалось загрузить модель: {ex.Message}");
                return null;
            }
        }

        private static bool HasTrigDetection(NirsSignalProcessing signalProc, NirsSignalData signalData)
        {
            for (int i = 0; i < 8; i++)
            {
                if (signalProc.GetTrigDetection(signalData, i))
                    return true;
            }
            return false;
        }

        private void StartRecording()
        {
            if (_Pipeline1.Sensor is { IsStarted: true })
            {
                _Pipeline1.CsvStreamer = new ReportsStreamerCsv(BuildReportPath("_Nirs1.csv"));
                _Pipeline1.CsvStarted = true;
            }

            if (_Pipeline2.Sensor is { IsStarted: true })
            {
                _Pipeline2.CsvStreamer = new ReportsStreamerCsv(BuildReportPath("_Nirs2.csv"));
                _Pipeline2.CsvStarted = true;
            }

            IsRecording = _Pipeline1.CsvStarted || _Pipeline2.CsvStarted;
        }

        private void StopRecording()
        {
            if (_Pipeline1.CsvStarted)
            {
                _Pipeline1.CsvStarted = false;
                _Pipeline1.CsvStreamer.Dispose();
                _Pipeline1.CsvStreamer = null;
            }

            if (_Pipeline2.CsvStarted)
            {
                _Pipeline2.CsvStarted = false;
                _Pipeline2.CsvStreamer.Dispose();
                _Pipeline2.CsvStreamer = null;
            }

            IsRecording = false;
        }

        private static string BuildReportPath(string suffix)
        {
            return Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath,
                                DataHelpers.GetCurrentDateTimeStr() + suffix);
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Один тракт «датчик → обработка → очередь отрисовки → отчёт».
        /// </summary>
        private sealed class NirsPipeline
        {
            public NirsSensorDevice Sensor;
            public NirsSignalProcessing SignalProcessing;
            public readonly Queue<NirsSignalData> Queue = new Queue<NirsSignalData>();
            public ReportsStreamerCsv CsvStreamer;
            public volatile bool CsvStarted;
        }

        #endregion
    }
}
