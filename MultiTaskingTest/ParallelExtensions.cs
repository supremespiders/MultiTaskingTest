using System.Diagnostics;
using System.Text.Json;

namespace MultiTaskingTest;

public static class ParallelExtensions
{
    public static async Task<List<T>> Load<T>(this string path)
    {
        return JsonSerializer.Deserialize<List<T>>(await File.ReadAllTextAsync(path));
    }
    public static async Task Save<T>(this List<T> items, string path=null)
    {
        var name = typeof(T).Name;
        if (path != null) name = path;
        await File.WriteAllTextAsync(name,JsonSerializer.Serialize(items)) ;
    }
    
    private static async Task<List<T2>> P<T, T2>(this IReadOnlyList<T> inputs, int threads, Func<T, Task<T2>> work, bool resume = true) where T:IInput
    {
        var sw = new Stopwatch();
        sw.Start();
        var outputs = new List<T2>();
        Notifier.Display("Start working");
        var tasks = new List<Task<T2>>();
        var taskUrls = new Dictionary<int, string>();
        var completedUrls = new List<string>();

        if (!resume)
        {
            if(File.Exists("completed")) File.Delete("completed");
            if(File.Exists("output")) File.Delete("output");
        }
        
        if (File.Exists("completed"))
        {
            completedUrls = (await File.ReadAllLinesAsync("completed")).ToList();
            inputs = inputs.Where(x => !completedUrls.Contains(x.Url)).ToList();
        }

        if (File.Exists("output")) 
            outputs =await "output".Load<T2>();

        if (inputs.Count == 0)
        {
            Notifier.Display($"No new inputs");
            return outputs;
        }

        var i = 0;
        do
        {
            if (i < inputs.Count)
            {
                var item = inputs[i];
                Notifier.Display($"Working on {i + 1} / {inputs.Count} , Total collected : {outputs.Count}");
                var t = work(item);
                tasks.Add(t);
                taskUrls.Add(t.Id, item.Url);
                i++;
            }

            if (tasks.Count != threads && i < inputs.Count) continue;
            try
            {
                var t = await Task.WhenAny(tasks).ConfigureAwait(false);
                completedUrls.Add(taskUrls[t.Id]);
                tasks.Remove(t);
                outputs.Add(await t);
            }
            catch (TaskCanceledException)
            {
                await File.WriteAllLinesAsync("completed", completedUrls);
                await outputs.Save();
                Notifier.Display($"Canceled.. we persisted the data");
                throw;
            }
            catch (Exception e)
            {
                Notifier.Error($"{(e is KnownException ? e.Message : e.ToString())}");
                var t = tasks.FirstOrDefault(x => x.IsFaulted);
                tasks.Remove(t);
            }

            if (tasks.Count == 0 && i == inputs.Count) break;
        } while (true);


        await File.WriteAllLinesAsync("completed", completedUrls);
        await outputs.Save();

        Notifier.Display($"Work completed, collected : {outputs.Count} in {sw.Elapsed.TotalMinutes:#0.00} min");
        return outputs;
    }

    public static async Task<List<T2>> Parallel<T, T2>(this IReadOnlyList<T> inputs, Func<T, Task<T2>> work, int threads, bool resume = true)where T:IInput
    {
        return await P(inputs, threads, work,resume);
    }

    public static async Task<List<T2>> Parallel<T, T2>(this IReadOnlyList<T> inputs, Func<T, Task<List<T2>>> work, int threads, bool resume = true)where T:IInput
    {
        return (await P(inputs, threads, work,resume)).SelectMany(x => x).ToList();
    }
}