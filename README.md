# TelemetryGrid.SCADA

Начальная реализация задания Алибаева Исляма, модуль 01.

## Структура

- `src/TelemetryGrid.Domain` — доменные модели телеметрии.
- `src/TelemetryGrid.Application` — потокобезопасный `BoundedChannel`, пакетная обработка раз в 100 мс и остановка через `CancellationToken`.
- `src/TelemetryGrid.WinForms` — пассивное WinForms-представление монитора с безопасным переходом в UI-поток.
- `tests/TelemetryGrid.Tests` — тесты валидации и доставки пакетов.

## Проверка

Требуется .NET 8 SDK:

```powershell
dotnet restore TelemetryGrid.SCADA.sln
dotnet test TelemetryGrid.SCADA.sln --configuration Release
dotnet run --project src/TelemetryGrid.WinForms --configuration Release
```

В текущем окружении .NET SDK не установлен, поэтому сборка и запуск пока не выполнены. Исходники и тесты подготовлены; следующим этапом нужно установить .NET 8 SDK и проверить критерии нагрузки модуля.
