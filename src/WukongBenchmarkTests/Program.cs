using WukongBenchmarkTests.Benchmarking;

var csvPath =
    @"C:\Users\ars04\Desktop\wukong-benchmark-tests\research\live-presentmon-smoke.csv";

var runner =
    new PresentMonRunner();

Console.WriteLine(
    "Запускаем live-захват PresentMon...");

runner.StartCapture(
    csvPath,
    "Code.exe");

for (var second = 1;
     second <= 6;
     second++)
{
    await Task.Delay(
        TimeSpan.FromSeconds(1));

    var length =
        File.Exists(csvPath)
            ? new FileInfo(csvPath).Length
            : 0;

    Console.WriteLine(
        $"{second} с: размер CSV = {length} байт");
}

Console.WriteLine(
    "Останавливаем PresentMon...");

await runner.StopCaptureAsync();

Console.WriteLine(
    "Захват завершён.");

var rows =
    File.ReadLines(csvPath)
        .Count();

Console.WriteLine(
    $"Строк в CSV вместе с заголовком: {rows}");