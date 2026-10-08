# Black Myth: Wukong Benchmark Automation

Автоматизированный инструмент на C#/.NET для двух проходов  
**Black Myth: Wukong Benchmark Tool** с CPU- и GPU-профилями.

> Важно: программе нужен уже существующий `GameUserSettings.ini`.
> Если Benchmark Tool ещё ни разу не запускался, сначала вручную запустите его один раз и проведите первый benchmark, чтобы конфиг был создан.

## Что делает программа

После запуска инструмент автоматически:

- находит установленный Wukong Benchmark Tool в библиотеках Steam;
- проверяет окружение;
- собирает характеристики компьютера;
- сохраняет исходный `GameUserSettings.ini`;
- применяет CPU-профиль;
- запускает benchmark;
- собирает frame-time через PresentMon;
- рассчитывает FPS;
- закрывает Wukong;
- применяет GPU-профиль;
- повторяет benchmark;
- восстанавливает исходный конфиг;
- выводит результат в консоль;
- сохраняет JSON-отчёт.

После запуска ручные действия пользователя не требуются.

---

## Требования

- Windows
- .NET 10 SDK
- Steam
- Black Myth: Wukong Benchmark Tool
- Intel PresentMon Console
- права администратора или членство в `Performance Log Users`
- существующий `GameUserSettings.ini`

Установка PresentMon:

```powershell
winget install -e --id Intel.PresentMon.Console
```

---

## Запуск

Сборка:

```powershell
dotnet build .\src\WukongBenchmarkTests
```

Проверка окружения без запуска benchmark:

```powershell
dotnet run --project .\src\WukongBenchmarkTests -- --preflight-only
```

Полный автоматический запуск:

```powershell
dotnet run --project .\src\WukongBenchmarkTests
```

---

## CPU-профиль

CPU-проход использует:

- native output resolution;
- render scale 50%;
- минимальные графические настройки;
- Frame Generation OFF;
- Ray Tracing OFF;
- VSync OFF;
- Dynamic Resolution OFF;
- FPS Limit OFF;
- Motion Blur OFF.

### Почему

Задача CPU-теста — максимально снизить влияние видеокарты.        
Снижение render scale и качества графики уменьшает GPU bottleneck, поэтому итоговый FPS сильнее зависит от производительности процессора.        
Нативное выходное разрешение сохраняется, чтобы интерфейс benchmark оставался стабильным для автоматизации.

---

## GPU-профиль

GPU-проход использует:

- native output resolution;
- render scale 100%;
- высокие графические настройки;
- Frame Generation OFF;
- Ray Tracing OFF;
- VSync OFF;
- Dynamic Resolution OFF;
- FPS Limit OFF.

### Почему

Здесь цель обратная — максимально загрузить видеокарту.        
100% render scale и высокий уровень графики делают GPU основным ограничивающим компонентом.        
Frame Generation отключён, чтобы FPS отражал реально отрендеренные кадры, а не сгенерированные промежуточные.        
Ray Tracing отключён для более универсального и воспроизводимого сценария на разных видеокартах.

---

## Как проходит тест

Основной сценарий:

```text
Поиск Wukong
1. Preflight
2. Сбор характеристик
3. CPU profile
4. CPU benchmark
5. Закрытие процессов
6. GPU profile
7. GPU benchmark
8. Восстановление конфига
9. Console + JSON report
```

Между двумя проходами программа ждёт полного завершения Wukong, launcher и PresentMon, чтобы избежать проблем при повторном запуске через Steam.

---

## Результаты

В консоль выводится информация и результат, который удобно читать:

```text
CPU: Intel(R) Core(TM) Ultra 9 275HX
Ядра / потоки: 24 / 24

GPU 1: Intel(R) Graphics
  Драйвер: 32.0.101.8424
GPU 2: NVIDIA GeForce RTX 5070 Ti Laptop GPU
  Драйвер: 32.0.15.9613

RAM: 31,37 ГБ
OS: Microsoft Windows 10.0.26200
Архитектура: X64

...статус и этапы CPU + GPU проходов бенчмарка...

CPU:
  Средний FPS: 153.32
  Минимальный FPS: 62.00
  Максимальный FPS: 220.00
  5-й перцентиль: 138.00
  Количество кадров: 24215
  Длительность: 150,529 с

GPU:
  Средний FPS: 41.28
  Минимальный FPS: 35.00
  Максимальный FPS: 47.00
  5-й перцентиль: 37.00
  Количество кадров: 6285
  Длительность: 152,141 с
```

Дополнительно создаётся также JSON:

```text
results/wukong-benchmark-YYYYMMDD-HHMMSS-fff.json
```

В нём сохраняются:
- характеристики системы;
- настройки CPU-профиля;
- настройки GPU-профиля;
- FPS-метрики;
- длительность тестов;
- количество кадров.

---

## Безопасность

Исходный `GameUserSettings.ini` сохраняется до начала тестов и восстанавливается в `finally`.

Восстановление выполняется даже при:

- ошибке;
- timeout;
- сбое UI;
- `Ctrl+C`.

После восстановления SHA-256 исходного и восстановленного файла сравниваются.        
Если хэши отличаются, программа завершает работу с ошибкой.

---

## Формат результата

Консоль используется для быстрого и удобного чтения.        
JSON нужен для хранения, сравнения и дальнейшей обработки результатов.

## Автоматические тесты

Для ключевой логики добавлены unit-тесты на xUnit.

Проверяются:

- формирование и округление итоговых FPS-метрик;
- настройки CPU- и GPU-профилей;
- изменение Frame Generation;
- backup и побайтовое восстановление `GameUserSettings.ini`.

Запуск:

```powershell
dotnet test .\tests\WukongBenchmarkTests.Tests
```

## Автор

**Arsen Mikailov** - @ars.mik        
**Email:** Arsen.0mikailov@ya.ru        
**GitHub:** [github.com/Ars-Mik](https://github.com/Ars-Mik)
