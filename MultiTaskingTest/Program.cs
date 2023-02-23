using HtmlAgilityPack;
using MultiTaskingTest;
using static System.Threading.Tasks.Task;

var cancelSource = new CancellationTokenSource();
Notifier.OnDisplay += (_, s) => Log(s, ConsoleColor.Green, true);
Notifier.OnError += (_, s) => Log(s, ConsoleColor.Red);

try
{
    CloseHandler.OnClose(() => Log("CLosing new!",ConsoleColor.Gray));
    // var scraper = new MyTekScraper();
    // await scraper.Main();
    // var urls = Enumerable.Range(1, 1000).Select(x => new Input { Url = $"site {x}" }).ToList();
    // var result = await urls.Parallel(x => Work(x, cancelSource.Token), 40, false);
    var doc = new HtmlDocument();
    doc.LoadHtml(File.ReadAllText("html.html"));
    var op = new XpathOptimizer();
    //op.Optimize("/html/body/div[2]/main/div[2]/div[1]/div[1]/div[1]/h1/span",doc);
    //op.Optimize("/html/body/div[2]/main/div[2]/div[1]/div[1]/div[3]/div[1]/span",doc);
    op.Optimize("//*[@id=\"maincontent\"]/div[2]/div[1]/div[1]/div[3]/div[2]/div",doc);
}
catch (TaskCanceledException)
{
    Log("Canceled",ConsoleColor.Blue);
}
catch (Exception e)
{
    Log(e is KnownException  ? e.Message: e.ToString());
}

async Task<Output> Work(Input input, CancellationToken ct)
{
    await Delay(1000, ct);
    return new Output() { Url = input.Url, Name = "Riadh" };
}

Console.ReadLine();

void Log(string s, ConsoleColor c = ConsoleColor.White, bool sameLine = false)
{
    var oldColor = Console.ForegroundColor;
    Console.ForegroundColor =c;
    if (sameLine)
        Console.Write($"\r{s}");
    else
    {
        if (Console.CursorLeft != 0) Console.WriteLine();
        Console.WriteLine(s);
    }

    Console.ForegroundColor = oldColor;
}