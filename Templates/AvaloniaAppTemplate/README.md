# Avalonia MVVM App — шаблон проекта

Шаблон `dotnet new`, собранный на инфраструктуре `NIRS_Demonstrator`, но
очищенный от предметной части (НИРС, DSP, оборудование).

Доступен и в командной строке, и в окне «Создать проект» Visual Studio,
и в Rider.

Созданное решение собирается и запускается сразу: две страницы с анимацией
перехода, переключение тем и языков, рабочие диалоги, журнал и контейнер
зависимостей.

## Установка

```
tools\install-template.cmd
```

Перезапустите Visual Studio, затем «Создать проект» → поиск «Avalonia MVVM App».

Из командной строки:

```
dotnet new avalonia-mvvm-app -n MyApp
```

```
dotnet new avalonia-mvvm-app -n MyApp --Android true --iOS true
```

Снять шаблон: `tools\install-template.cmd -Uninstall`.

### Почему .cmd, а не .ps1 напрямую

На машине с политикой выполнения `AllSigned` или `Restricted` запуск
`.\tools\Install-Template.ps1` завершится ошибкой «файл не имеет цифровой
подписи». Проверить настройку: `Get-ExecutionPolicy -List`.

Обёртка `.cmd` вызывает PowerShell с `-ExecutionPolicy Bypass`, что действует
**только на этот процесс** и ничего не меняет в настройках машины. То же вручную:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Install-Template.ps1
```

## Что требуется на машине

| Нужно | Для чего |
|---|---|
| **.NET 10 SDK** | Все проекты собираются под `net10.0`. |
| Рабочая нагрузка `android` для .NET 10 | Голова Android: `dotnet workload install android`. |
| **Android SDK: платформа 36 и build-tools 36.x** | Голова Android. Рабочая нагрузка их не ставит. |
| **JDK 17–21** | Голова Android. Проверяется свойством `JavaSdkVersion` (по умолчанию 21.0.8), минимум `17.0`. |
| Рабочая нагрузка `ios` для .NET 10 | Голова iOS. Компиляция идёт и на Windows, упаковка — только на macOS. |
| Рабочая нагрузка `wasm-tools` | Браузерная голова. |
| Расширение «Avalonia for Visual Studio» **12.x** | Предпросмотр XAML, см. ниже. |

Проверить SDK и нагрузки: `dotnet --list-sdks`, `dotnet workload list`.

Две ловушки, на которые легко потратить время:

- `dotnet workload list` может показывать нагрузку установленной, хотя пакеты
  для нужной версии SDK отсутствуют. Признак — ошибка `NETSDK1147` при сборке;
  лечится `dotnet workload install android ios`.
- Рабочая нагрузка `android` — это не Android SDK. Если SDK нет или в нём нет
  платформы 36, сборка падает с `error XA5300`, а путь можно задать вручную:
  `dotnet build -p:AndroidSdkDirectory=<путь> -p:JavaSdkDirectory=<путь к JDK>`.

### Предпросмотр XAML: версия расширения должна совпадать с версией Avalonia

Шаблон закреплён на **Avalonia 12.1.5**, поэтому в Visual Studio нужна
**12.x версия расширения «Avalonia for Visual Studio»**. Проверить
установленную: Extensions → Manage Extensions → Installed.

С расширением **11.x предпросмотр проектов на Avalonia 12 не работает** —
и наоборот. Отказ выглядит так (пример для обратного случая, проект на
Avalonia 11 с расширением 12.x):

```
System.TypeLoadException: Could not load type
'Avalonia.Platform.Surfaces.IFramebufferPlatformSurface'
from assembly 'Avalonia.Base, Version=11.3.12.0'
Previewer process exited: -532462766
```

Причина не в проекте. В расширении лежит единственный
`HostApp\AvaloniaUI.Previewer.DesignerSupport.dll`, скомпилированный против
конкретной основной версии Avalonia; в 12-й версии тип
`IFramebufferPlatformSurface` переехал в пространство имён
`Avalonia.Platform.Surfaces`, поэтому previewer одной линейки не поднимает
приложение другой. Против какой версии собран установленный previewer,
видно так:

```powershell
$p = "$env:LOCALAPPDATA\Microsoft\VisualStudio\<экземпляр>\Extensions\<папка>\HostApp\AvaloniaUI.Previewer.DesignerSupport.dll"
[Reflection.Assembly]::ReflectionOnlyLoadFrom($p).GetReferencedAssemblies() |
    Where-Object Name -like 'Avalonia*' | Select-Object Name, Version
```

Нужные версии расширения доступны на вкладке Version History в Marketplace.
Если понадобится вернуться на линейку 11.x, последняя в ней — `11.12.2.0`
(март 2026), и после её установки стоит снять автообновление именно этого
расширения, иначе VS вернёт 12.x.

### Вторая причина отказа: имя метода в точке входа

```
System.InvalidOperationException: Unable to create AppBuilder from type
"MyApp.Desktop.Program". Input type either needs to have
BuildAvaloniaApp -> AppBuilder method or inherit Application type.
```

Превьюер берёт тип точки входа и вызывает `AppBuilder.Configure(Type)`, а тот
ищет **открытый статический метод ровно с именем `BuildAvaloniaApp`**,
возвращающий `AppBuilder`. Никакого атрибута или настройки здесь нет — только
совпадение имени. Переименуете метод — предпросмотр отвалится, хотя приложение
будет собираться и запускаться как ни в чём не бывало.

## Параметры шаблона

| Параметр | По умолчанию | Назначение |
|---|---|---|
| `--Desktop` | `true` | Точка входа для Windows, Linux, macOS |
| `--Android` | `false` | Точка входа для Android |
| `--iOS` | `false` | Точка входа для iOS |
| `--Browser` | `false` | Точка входа для браузера (WebAssembly) |
| `--CompanyName` | пусто | Добавляется в путь к каталогам настроек, журнала и отчётов |

Невыбранные платформы не попадают ни в каталог решения, ни в файл `.sln`.

Сборка мобильных и браузерной голов требует рабочих нагрузок:

```
dotnet workload install android ios wasm-tools
```

Для Android дополнительно нужны Android SDK и JDK, для полной сборки iOS — macOS.

## Состав

| Проект | Назначение |
|---|---|
| `{Name}.Core` | Платформонезависимое ядро: MVVM, журнал, файлы, задачи, контейнер зависимостей. Не ссылается на Avalonia. |
| `{Name}` | Приложение: страницы, стили, темы, языки, диалоги, преобразователи, анимация. |
| `{Name}.Desktop` | Windows, Linux, macOS. |
| `{Name}.Android` | Android. |
| `{Name}.iOS` | iOS. |
| `{Name}.Browser` | Браузер (WebAssembly). |

Что входит:

- **Font Awesome** — `Optris.Icons.Avalonia.FontAwesome`, имена иконок
  вынесены в `Styles/Fonts.axaml`, стили кнопок `IconButton` и `IconGrowButton`.
- **Дерево стилей с переключением тем и языков** — `Styles/Colors.axaml`,
  `Styles/Fonts.axaml`, `Styles/Buttons.axaml`, `Styles/Themes/*`,
  `Styles/Languages/*`; реестры и применение — в `AppConfig`.
- **Многостраничность с анимацией** — `BasePage`, `PageHost` на `Carousel`,
  реестр страниц `Navigation/PageRegistry`.
- **Диалоги** — `IUIManager` в ядре, реализация `IoC/UIManager` на `DialogHost`,
  представления `Dialogs/MessageDialog`, `ConfirmDialog`, `ChoiceDialog`.
- **Свой MVVM, журнал и файловый слой** — всё в `{Name}.Core`.
- **Преобразователи значений** — `BaseValueConverter` плюс набор готовых.
- **IoC, ViewModelLocator, UIManager** — `Core/IoC`, `ViewModels/ViewModelLocator`.
- **Анимация** — `Helpers/Animation/PropertyAnimator` и типизированные обёртки.

Все платформы используют одну разметку: на настольной работает ветка
`IClassicDesktopStyleApplicationLifetime` (окно `MainWindow`), на Android, iOS
и в браузере — `ISingleViewApplicationLifetime` (представление `MainView`).

## Проверка шаблона

После правки шаблона прогоняйте проверку — она создаёт решение и собирает его:

```
tools\test-template.cmd
```

Проверяются: подстановка имени во всех файлах, обработка директив `#if` в `.sln`
и сборка. С ключом `-AllPlatforms` в решение добавляются мобильные головы
(нужны рабочие нагрузки).

## Структура шаблона

```
content/
  .template.config/
    template.json          параметры, условное включение платформ
    ide.host.json          отображение параметров в Visual Studio
  AvaloniaMvvmApp.sln
  AvaloniaMvvmApp/         приложение
  AvaloniaMvvmApp.Core/    ядро
  AvaloniaMvvmApp.Desktop/
  AvaloniaMvvmApp.Android/
  AvaloniaMvvmApp.iOS/
  AvaloniaMvvmApp.Browser/
tools/
  Install-Template.ps1     установка и снятие
  Test-Template.ps1        проверка сборкой
  *.cmd                    обёртки для машин с ограниченной политикой выполнения
```

`sourceName` в `template.json` — строка `AvaloniaMvvmApp`: она заменяется на имя
проекта в содержимом файлов, в именах файлов и каталогов.

> **Не переименовывайте `sourceName` в `AvaloniaApp`.** Эта строка входит
> подстрокой в тип `AvaloniaAppDelegate` из `Avalonia.iOS`, и подстановка
> испортила бы код головы iOS. Выбирая новое значение, убедитесь, что оно не
> встречается внутри идентификаторов фреймворка.

Есть имена, которые подстановка обязана пропустить, потому что их ищут по
строковому совпадению извне: `BuildAvaloniaApp` в точках входа (его ищет
конструктор предварительного просмотра разметки) и `AvaloniaAppDelegate` в
голове iOS. `Test-Template.ps1` проверяет оба в созданном решении — на этом уже
ловились две ошибки подряд, так что проверку лучше не выбрасывать.

Файлы подхватываются по содержимому каталога — перечислять их где-либо не нужно.

## Как добавлять своё

### Страница

1. Допишите значение в `ApplicationPage` (проект Core).
2. Скопируйте пару `Pages/MainPage.axaml` + `.axaml.cs` и модель представления.
3. Зарегистрируйте страницу в `PageRegistry.RegisterDefaults`:

```csharp
Register(ApplicationPage.Reports,
         vm => new ReportsPage(vm as ReportsPageViewModel),
         PageTransitionAxis.Horizontal,
         order: 1);
```

`order` задаёт направление анимации: переход к большему порядку едет вперёд,
к меньшему — назад. Главная страница — `0`, служебные вроде настроек —
отрицательные. `PageTransitionAxis.Vertical` переключает ось на вертикальную.

Больше ничего править не нужно: `PageHost` берёт параметры перехода из реестра.

### Цветовая тема

Положите словарь в `Styles/Themes/` с тем же набором ключей `Theme_*`, что в
`LightTheme.axaml`, и допишите строку в `App.RegisterThemesAndLanguages`.
Страница настроек подхватит тему сама.

### Язык

Положите словарь в `Styles/Languages/` с тем же набором ключей и допишите строку
в `App.RegisterThemesAndLanguages`.

В разметке строки берутся через `{DynamicResource ИмяКлюча}` — именно
`DynamicResource`, иначе интерфейс не обновится при переключении языка.
В коде — через `AppResources.GetString("ИмяКлюча")`.

### Диалог

Наследуйте `BaseDialog<TResult>`, закрывайте через `Close(результат)`
и показывайте через `UIManager.ShowDialogAsync`.

### Служба в контейнере

Регистрация — в `App.ConfigureServices`. Модели представления страниц
регистрировать не обязательно: `IoC.Get<T>` создаст их через контейнер,
внедрив зарегистрированные зависимости.

## Чем шаблон отличается от NIRS_Demonstrator

Инфраструктура переработана там, где исходный код был привязан к конкретному
приложению либо содержал дефекты.

| Было | Стало | Причина |
|---|---|---|
| Два контейнера зависимостей: Ninject в `IoC` и `ServiceProvider` в `App` | Один — Microsoft.Extensions.DependencyInjection, фасад `IoC` сохранён | Две системы внедрения в одном приложении расходятся в поведении; Ninject-зависимость убрана |
| `ViewModelBase : ReactiveObject` с собственным `PropertyChanged`, перекрывающим базовый | Чистая реализация `INotifyPropertyChanged` с `SetProperty` | Перекрытие события базового класса — источник трудноуловимых сбоев привязки; ReactiveUI больше не нужен |
| `switch` в `ApplicationPageHelpers` — страница→представление и обратно | Реестр `PageRegistry` | Добавление страницы правило два `switch`; теперь — одна регистрация |
| `PageHost` знал про `ApplicationPage.Settings` и `Main`, задержки `Task.Delay(500)` | Ось и направление из реестра, задержка равна длительности анимации | Контейнер страниц не должен знать конкретные страницы |
| `AppConfig`: фиксированные словари тем и языков, пути `avares://NIRS_Demonstrator/...`, поля `DeviceMac`, `UartSpeed`, каталоги словарей и моделей | Реестры тем и языков, пути вычисляются от имени приложения, предметные поля убраны | Шаблон не должен тащить чужую предметную область |
| `AppConfig.Dispose` пустой; `RegisterDisposableObject` без парного снятия | `Dispose` освобождает и очищает список, добавлен `UnregisterDisposableObject` | Объекты накапливались и не собирались сборщиком мусора |
| `AppConfig.InitializeAppConfig` и `LoadAppConfig` — `async void` | Синхронная инициализация | `async void` не даёт дождаться завершения и съедает исключения |
| `FileLogger` писал через `IoC.File` | Пишет сам | Журнал создаётся раньше, чем собран контейнер: обращение к `IoC.File` падало |
| `UIManager` и `IUIManager` — полностью закомментированы, `ConfirmDialog` нигде не показывался | Рабочая реализация на `DialogHost` и три представления диалогов | Диалоги в исходном проекте не работали |
| Три класса анимации с дублированным кодом, `async void` и ожиданием в цикле `while (...) await Task.Delay(1)` | Один `PropertyAnimator`, затвор на `SemaphoreSlim`, `FillMode.Forward` | Цикл ожидания жёг такты; `async void` терял исключения |
| `Theme_GreekBlueBrush` в тёмной теме и стилях | `Theme_AccentBrush`, одинаковый набор ключей в обеих темах | Остаток от другого проекта; в светлой теме ключа не было |
| `ApplicationViewModel` без истории | Добавлены `GoBack`, `CanGoBack`, `ClearHistory` | Кнопка «назад» нужна почти всегда |
| Команды всегда исполнимы | Добавлены условие `canExecute` и `AsyncRelayCommand` | Асинхронные обработчики писались как `async void` |
| Голова Android: `Label = "UartSnifer.Android"` | `Label = "{Name}"` | Остаток от другого проекта |

Также добавлено: `ISettingsStore` с реализацией на XML, `AppResources` для
доступа к строкам из кода, набор готовых преобразователей значений,
`app.manifest` с объявлением поддержки масштабирования на каждом мониторе.

## Что проверено

На решении, созданном по шаблону:

- **сборка** настольной конфигурации — без ошибок и предупреждений;
- **сборка головы iOS** — `dotnet build` под Windows проходит без ошибок и
  предупреждений (`net10.0-ios`, платформа 27.1, вывод в
  `bin/Debug/net10.0-ios/iossimulator-x64`);
- **сборка браузерной головы** — успешно, с единственным предупреждением о
  несвязанных нативных ссылках (ожидаемо без `WasmBuildNative`/AOT);
- **восстановление головы Android** — без предупреждений (см. выравнивание
  AndroidX.Lifecycle в `.csproj`: без него восстановление даёт девять `NU1608`);
- **компиляция головы Android** — исходники собраны компилятором напрямую
  против установленных эталонных сборок `Mono.Android`
  (`Microsoft.Android.Ref.36`, 36.1.118), `Avalonia.Android` и полного
  замыкания AndroidX (239 ссылок): ошибок нет;
- **подстановка имени** — ни одного остатка строки `AvaloniaMvvmApp`;
- **условное включение платформ** — невыбранные головы отсутствуют и в каталоге,
  и в `.sln`; директивы `#if` в `.sln` обработаны;
- **запуск приложения** — без сбоя;
- **переход** «Главная → Настройки» и возврат через историю;
- **смена темы** — значения ключей `Theme_*` после переключения соответствуют
  тёмной и светлой теме;
- **смена языка** — строки читаются из обоих словарей;
- **файлы настроек и журнала** создаются в каталогах, вычисленных `AppConfig`;
- **диалог** появляется в дереве элементов после `IUIManager.ShowConfirmAsync`;
- **точка входа для предпросмотра разметки** — вызов `AppBuilder.Configure(Type)`
  на типе точки входа собранной головы Desktop возвращает `AppBuilder`
  (тот самый вызов, которым превьюер поднимает приложение). Проверка с
  негативным контролем: после переименования `BuildAvaloniaApp` тот же вызов
  падает с `InvalidOperationException`.

Проверка выполняется `tools\test-template.cmd`; проверки в среде выполнения
и компиляция мобильных голов — отдельными разовыми прогонами, в шаблон они
не входят.

Не проверялось:

- **сборка головы Android**: на машине разработки нет платформы Android 36
  (есть только 28–30) и нет JDK 17–21, поэтому `dotnet build` падает с
  `error XA5300`. Проверена компиляция кода, но не слияние манифеста,
  не ресурсы `values-v31` (включая атрибут `postSplashScreenTheme`) и не
  создание `MainApplication` средой выполнения Android;
- **упаковка iOS**: требует macOS. Компиляция под Windows проходит, подписи и
  сборки пакета не было;
- сборка под Linux и macOS;
- создание проекта из окна Visual Studio (проверено только через `dotnet new`);
- работа приложения на самих Android и iOS.

## Версии пакетов

Версии указаны прямо в `.csproj`, без централизованного управления
(`Directory.Packages.props`): так шаблон не зависит от файлов уровня решения.

| Пакет | Версия |
|---|---|
| Avalonia и платформенные пакеты | 12.1.5 |
| AvaloniaDialogs | 3.7.1 |
| Optris.Icons.Avalonia.FontAwesome | 12.0.7 |
| AvaloniaUI.DiagnosticsSupport | 2.2.3 |
| Microsoft.Extensions.DependencyInjection | 10.0.14 |
| Xamarin.AndroidX.Core.SplashScreen (Android) | 1.2.0.3 |
| Xamarin.AndroidX.Lifecycle.* (Android) | 2.11.0.1 — выравнивание, см. комментарий в `.csproj` |

Целевые платформы: `net10.0` (общий проект, Core, Desktop),
`net10.0-android36.0`, `net10.0-ios`, `net10.0-browser`.

Минимальные версии ОС: Android 23 (`SupportedOSPlatformVersion`), iOS 15.0.
Для iOS это не произвольное число: пакет `Microsoft.iOS.Sdk.net10.0_27.1`
объявляет минимум `15.0`, и более низкое значение сборка под Windows
пропускает молча, а полная сборка на macOS отвергает.

Платформенные пакеты Avalonia подключены в точках входа, а не в общем проекте:
иначе голова Android тянула бы настольные серверы вывода.

### Чем отличается от сборки под Avalonia 11

Шаблон переведён с Avalonia 11.3.12; перечислено то, что пришлось изменить —
пригодится, если такой же переход нужно сделать в своём проекте.

- **Пакет иконок заменён.** `Projektanker.Icons.Avalonia` объявлен автором
  заброшенным (issue #233), его последняя версия 9.6.2 зависит от
  Avalonia 11.2.8, а issue #232 называется «Crashes in Avalonia 12». Взят
  преемник `Optris.Icons.Avalonia.FontAwesome`. API совпадает —
  `IconProvider.Current.Register<FontAwesomeIconProvider>()` и элемент
  `<i:Icon Value="…" />`, — поменялись только пространства имён: `using` в
  точках входа и `xmlns:i` в `Styles/Buttons.axaml`.
- **Android и iOS требуют .NET 10.** Для этих платформ Avalonia 12 иных
  вариантов не поддерживает. Остальные проекты тоже переведены на `net10.0`
  ради единообразия.
- **Голова Android переписана.** `AvaloniaMainActivity<TApp>` стал неgeneric
  `AvaloniaMainActivity`, а настройка `AppBuilder` переехала в отдельный класс
  `MainApplication : AvaloniaAndroidApplication<App>` с атрибутом
  `[Application]` (файл `MainApplication.cs`). Регистрация `IconProvider`
  перенесена туда же, потому что `CustomizeAppBuilder` из активности убран.
- **Добавлена ветка `IActivityApplicationLifetime`** в
  `App.OnFrameworkInitializationCompleted`: на Android представление отдаётся
  фабрикой `MainViewFactory`, а не присваивается в `MainView`. Проверка идёт
  раньше `ISingleViewApplicationLifetime`, которая осталась для iOS и
  браузера.
- **`AppConfig.SetApplicationLifetime(ISingleViewApplicationLifetime)` заменён
  на `AttachToMainView(Control)`**: при фабрике представления к моменту
  настройки жизненного цикла представления ещё не существует.
- **Компилируемые привязки включены по умолчанию** —
  `AvaloniaUseCompiledBindingsByDefault` в Avalonia 12 равно `true`, то есть
  каждый `{Binding}` в XAML становится `CompiledBinding`. Шаблон и так ставил
  это свойство явно, поэтому поведение не изменилось.

Ломающих изменений Avalonia 12 шаблон больше нигде не касается: поиск по
исходникам не нашёл ни одного обращения к `IStyleable`,
`ResourcesChangedEventArgs`, `AttachDevTools`, `FuncMultiValueConverter`,
`SystemDecorations`, `TextBox.Watermark`, `PlacementMode`, `IDataObject`,
файловым диалогам `OpenFileDialog`/`SaveFileDialog`, `ItemContainerGenerator`,
`IBinding`/`InstancedBinding`, `BindingPlugins`, `CustomAnimatorBase`,
`CubicBezierEasing` и прочему из списка на
<https://docs.avaloniaui.net/docs/avalonia12-breaking-changes>.

## История: почему не .vstemplate

Первая версия шаблона была сделана в формате Visual Studio (`.vstemplate`,
многопроектный `ProjectGroup`). Visual Studio создавала проекты и подставляла
имена, но **не копировала ни одного файла**: вложенные `<Folder>`/`<ProjectItem>`
игнорировались, а кириллица в комментариях портилась при перекодировании.

Официальное расширение Avalonia для Visual Studio решает ту же задачу иначе:
его `.vstemplate` — плоская обёртка с мастером, который вызывает шаблоны
`dotnet new`. Шаблон переведён на тот же механизм: он проверяется из командной
строки целиком и при этом остаётся доступным в окне «Создать проект».
