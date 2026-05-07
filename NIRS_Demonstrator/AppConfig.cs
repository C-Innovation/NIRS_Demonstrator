using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.Views;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class AppConfig : IDisposable
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        protected Collection<IDisposable> _objectsForDispose;

        #endregion

        #region Private Members
        private static AppConfig _instance = null;
        private string _appConfigDirPath;// = Application.Current.StartupUri + "\\.sys";
        private string _appConfigFilePath;
        private AppSettingsData _appSettingsData;

        #endregion

        #region Public Properties
        public const string DARK_STYLE_PATH = "avares://NIRS_Demonstrator/Styles/Themes/DarkTheme.axaml";
        public const string LIGHT_STYLE_PATH = "avares://NIRS_Demonstrator/Styles/Themes/LightTheme.axaml";
        public const string LANG_ENGLISH = "avares://NIRS_Demonstrator/Styles/Languages/en-US.axaml";
        public const string LANG_RUSSIAN = "avares://NIRS_Demonstrator/Styles/Languages/ru-RU.axaml";
        public const string LANG_GREEK = "avares://NIRS_Demonstrator/Styles/Languages/el-GR.axaml";
        public const string APP_CONFIG_FILE_NAME = "sys.xml";

        public const string AI_FOLDER_NAME = "AI";
        public const string AI_MODELS_FOLDER_NAME = "Models";


        public Dictionary<AppTheme, string> THEMES { get; } = new Dictionary<AppTheme, string>()
        {
            { AppTheme.Light,   LIGHT_STYLE_PATH },
            { AppTheme.Dark,    DARK_STYLE_PATH  },
        };

        public Dictionary<AppLanguage, string> LANGS { get; } = new Dictionary<AppLanguage, string>()
        {
            { AppLanguage.English,  LANG_ENGLISH },
            { AppLanguage.Russian,  LANG_RUSSIAN  },
        };

        public ResourceDictionary LastTheme { get; set; }
        public ResourceDictionary LastLang { get; set; }

        public string SettingsDirectoryPath { get; set; }
        public string DictionariesDirectoryPath { get; set; }
        public string AiDirectoryPath { get; set; }
        public string ReportsDirectoryPath { get; set; }

        public AppTheme AppTheme { get; private set; }
        public AppLanguage AppLanguage { get; private set; }
        public string ReportsDirPath { get; private set; }
        public string DeviceMac { get; private set; }
        public string DeviceName { get; private set; }
        public bool ConnectOnStart { get; private set; }
        public int UartSpeed { get; private set; }

        public Brush BackgroundMainBrush { get; set; }
        public Brush ForegroundMainBrush { get; set; }

        public Size MainViewSize { get; set; }

        public IControlledApplicationLifetime ApplicationLifetime { get; private set; }
        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        public event EventHandler<SizeChangedEventArgs> MainViewSizeChanged;
        public event EventHandler MainViewLoaded;
        public event EventHandler<int> WordInferenceComplete;
        public event EventHandler<string> OnShareDictionaryRequest;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        private AppConfig()
        {
            _objectsForDispose = new Collection<IDisposable> { };
        }

        #endregion

        #region Private Callbacks

        private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            foreach (var obj in _objectsForDispose)
            {
                obj.Dispose();
            }
        }
        private void MainView_Unloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            foreach (var obj in _objectsForDispose)
            {
                obj.Dispose();
            }
        }
        #endregion

        #region Public Methods

        public static AppConfig GetInstance()
        {
            if (_instance is null)
                _instance = new AppConfig();
            return _instance;
        }

        public void NotifyMainViewSizeChanged(MainView view, SizeChangedEventArgs arg)
        {
            MainViewSizeChanged?.Invoke(view, arg);
            MainViewSize = arg.NewSize;
        }

        public void NotifyMainViewLoaded(MainView view)
        {
            MainViewLoaded?.Invoke(view, EventArgs.Empty);
        }

        public void SetApplicationLifetime(IControlledApplicationLifetime applicationLifetime)
        {
            ApplicationLifetime = applicationLifetime;
            ApplicationLifetime.Exit += OnApplicationExit;
        }

        public void SetApplicationLifetime(ISingleViewApplicationLifetime applicationLifetime)
        {
            applicationLifetime.MainView.Unloaded += MainView_Unloaded; 
        }

        

        public async void InitializeAppConfig()
        {
            LoadAppConfig();

            BackgroundMainBrush = new SolidColorBrush((AppTheme == AppTheme.Dark) ? Color.FromArgb(0xff, 0x68, 0x68, 0x68) : Color.FromArgb(0xff, 0xff, 0xff, 0xff));
            ForegroundMainBrush = new SolidColorBrush((AppTheme == AppTheme.Dark) ? Color.FromArgb(0xff, 0xef, 0xef, 0xef) : Color.FromArgb(0xff, 0x68, 0x68, 0x68));
            InitTheme();
            InitLang();
            
            
        }

        public void ChangeTheme(AppTheme theme)
        {
            if (this.LastTheme == null)
                return;
            if (Application.Current.Resources.MergedDictionaries.Contains(this.LastTheme))
                Application.Current.Resources.MergedDictionaries.Remove(this.LastTheme);

            string style = AppConfig.GetInstance().THEMES[theme];
            var uri = new Uri(style);
            ResourceDictionary resourceDict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
            this.LastTheme = resourceDict;
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
            this.AppTheme = theme;
            BackgroundMainBrush = new SolidColorBrush((AppTheme == AppTheme.Dark) ? Color.FromArgb(0xff, 0x68, 0x68, 0x68) : Color.FromArgb(0xff, 0xff, 0xff, 0xff));
            ForegroundMainBrush = new SolidColorBrush((AppTheme == AppTheme.Dark) ? Color.FromArgb(0xff, 0xef, 0xef, 0xef) : Color.FromArgb(0xff, 0x68, 0x68, 0x68));
        }

        public void ChangeLang(AppLanguage lang)
        {
            if (this.LastLang == null)
                return;
            if (Application.Current.Resources.MergedDictionaries.Contains(this.LastLang))
                Application.Current.Resources.MergedDictionaries.Remove(this.LastLang);

            string style = AppConfig.GetInstance().LANGS[lang];
            var uri = new Uri(style);
            ResourceDictionary resourceDict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
            this.LastLang = resourceDict;
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
            this.AppLanguage = lang;


        }

        public void SetDeviceParams(string name, string mac, bool connectOnStart)
        {
            this.DeviceName = name;
            this.DeviceMac = mac;
            this.ConnectOnStart = connectOnStart;
        }

        public void SaveAppConfig()
        {
            AppSettings tConfData = new AppSettings();
            tConfData.ReportsDirPath = this.ReportsDirPath;
            tConfData.AppLanguage = this.AppLanguage;
            tConfData.AppTheme = this.AppTheme;
            tConfData.ConnectOnStart = this.ConnectOnStart;
            tConfData.UartSpeed = this.UartSpeed;
            tConfData.DeviceMac = this.DeviceMac;
            tConfData.DeviceName = this.DeviceName;
            //IoC.Application.SpeechLanguage = (int)this.SpeechLanguage;
            if (File.Exists(this._appConfigFilePath))
            {
                File.Delete(this._appConfigFilePath);
            }
            this._appSettingsData.Write(tConfData);
        }


        public void SendShareRequest(string path)
        {
            OnShareDictionaryRequest?.Invoke(this, path);
        }

        public void Dispose()
        {

        }
        
        public void RegisterDisposableObject(IDisposable disposable)
        {
            if(!_objectsForDispose.Contains(disposable)) 
                _objectsForDispose.Add(disposable);
        }

        #endregion

        #region Private Methods

        private async void LoadAppConfig()
        {
            CheckAppConfigFiles();

            AppSettings tConfData = this._appSettingsData.Read();

            if (tConfData is null)
            {

                File.Delete(this._appConfigFilePath);
                CheckAppConfigFiles();
                tConfData = this._appSettingsData.Read();
            }

            this.ReportsDirPath = tConfData.ReportsDirPath;
            this.AppLanguage = tConfData.AppLanguage;
            this.AppTheme = tConfData.AppTheme;
            this.DeviceMac = tConfData.DeviceMac;
            this.DeviceName = tConfData.DeviceName;
            this.ConnectOnStart = tConfData.ConnectOnStart;
            this.UartSpeed = tConfData.UartSpeed;


        }

        private void CheckAppConfigFiles()
        {
            //string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            //this._appConfigDirPath = Path.Combine(path, APP_SYS_SECTION, APP_MEDICOM_ROOT, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, APP_CONFIG_DIR_NAME);

            if (string.IsNullOrEmpty(SettingsDirectoryPath))
                throw new Exception("Invalid directory path. SettingsDirectoryPath is null");



            if (!Directory.Exists(SettingsDirectoryPath))
                Directory.CreateDirectory(SettingsDirectoryPath);

            if (!Directory.Exists(ReportsDirectoryPath))
                Directory.CreateDirectory(ReportsDirectoryPath);
            //throw new Exception("Invalid directory path. Directory is not exist");

            this._appConfigFilePath = Path.Combine(SettingsDirectoryPath, APP_CONFIG_FILE_NAME);
            this._appSettingsData = new AppSettingsData(this._appConfigFilePath);

            if (!File.Exists(_appConfigFilePath))
            {
                this._appSettingsData.Write(new AppSettings());
            }



        }

        private void InitTheme()
        {
            string style = AppConfig.GetInstance().THEMES[this.AppTheme];
            var uri = new Uri(style);
            ResourceDictionary resourceDict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
            this.LastTheme = resourceDict;
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }

        private void InitLang()
        {
            string style = AppConfig.GetInstance().LANGS[this.AppLanguage];
            var uri = new Uri(style);
            ResourceDictionary resourceDict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
            this.LastLang = resourceDict;
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }


        

        


        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    [XmlRoot("AppSettings")]
    [XmlType("AppSettings")]
    public class AppSettings
    {
        #region Public Properties

        [XmlAttribute("Version")]
        //[XmlElement("AppVersion")]
        public string AppVersion { get; set; }

        [XmlElement("ReportsDirPath")]
        public string ReportsDirPath { get; set; }

        [XmlElement("AppTheme")]
        public AppTheme AppTheme { get; set; }

        [XmlElement("AppLanguage")]
        public AppLanguage AppLanguage { get; set; }

        [XmlElement("DeviceMac")]
        public string DeviceMac { get; set; }

        [XmlElement("DeviceName")]
        public string DeviceName { get; set; }

        [XmlElement("ConnectOnStart")]
        public bool ConnectOnStart { get; set; }

        [XmlElement("UartSpeed")]
        public int UartSpeed { get; set; }
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public AppSettings()
        {
            ReportsDirPath = string.Empty;
            AppTheme = AppTheme.Light;
            AppLanguage = AppLanguage.Russian;
            AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DeviceMac = "NO_MAC";
            DeviceName = "NO_NAME";
            ConnectOnStart = false;
            UartSpeed = 115200;
        }

        #endregion

    }

    /// <summary>
    /// 
    /// </summary>
    public class AppSettingsData
    {

        #region Constants

        private const string ROOT_ELEMENT = "AppSettingsData";

        #endregion

        #region Private Members

        private string _dataPath;

        #endregion

        #region Constructor

        public AppSettingsData(string dataPath)
        {
            _dataPath = dataPath;
        }

        #endregion

        #region Public Methods

        public bool Write(AppSettings settings)
        {
            if (string.IsNullOrEmpty(this._dataPath))
                return false;

            if (settings == null)
                return false;

            return XmlSerialization.Serialize(this._dataPath, settings);
        }

        public AppSettings Read()
        {
            if (string.IsNullOrEmpty(this._dataPath))
                return null;

            return XmlSerialization.Deserialize<AppSettings>(this._dataPath);
        }

        #endregion
    }

    public enum AppTheme
    {
        Light = 1,
        Dark = 0,

    }

    public enum AppLanguage
    {
        English = 0,
        Russian = 1,
    }

    public enum SpeechLanguage
    {
        English = 0,
        Russian = 1,
    }
}
