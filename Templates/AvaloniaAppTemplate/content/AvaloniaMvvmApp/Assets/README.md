# Assets

Ресурсы приложения: изображения, иконки окна, шрифты, файлы данных.

Всё содержимое каталога включается в сборку как `AvaloniaResource` (см. `.csproj`),
поэтому обращаться к файлам можно так:

```xml
<Window Icon="/Assets/app-icon.ico" />
<Image Source="/Assets/logo.png" />
```

Из кода — через `avares://`:

```csharp
var uri = new Uri("avares://ИмяПроекта/Assets/logo.png");
using var stream = AssetLoader.Open(uri);
```

Этот файл можно удалить — он нужен лишь для того, чтобы каталог попал в шаблон.
