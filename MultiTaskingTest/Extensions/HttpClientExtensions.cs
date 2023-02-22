using System.Net;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;

namespace MultiTaskingTest.Extensions;

public static class HttpClientExtensions
{
    public static Dictionary<string, string> ParseCookieFromSet(this IEnumerable<string> cookiesSets)
    {
        var dic = new Dictionary<string, string>();
        foreach (var line in cookiesSets)
        {
            var s = line[..line.IndexOf(";", StringComparison.Ordinal)];
            var idx = s.IndexOf("=", StringComparison.Ordinal);
            var key = s[..idx];
            var val = s[(idx + 1)..];
            if (!dic.ContainsKey(key))
                dic.Add(key, val);
        }
        return dic;
    }

    private static async Task<Response> HandleAndRepeat(Func<Task<Response>> func,string url,CancellationToken ct=new (),int tries=1)
    {
        var lastError = "";
        for (int i = 0; i < tries; i++)
        {
            try
            {
                return await func();
            }
            catch (TaskCanceledException)
            {
                if (ct.IsCancellationRequested) throw;
            }
            catch (HttpRequestException ex)
            {
                lastError = ex.ToString();
            }
            catch (WebException ex)
            {
                lastError = ex.ToString();
                try
                {
                    lastError = await new StreamReader(ex.Response?.GetResponseStream() ?? throw new InvalidOperationException()).ReadToEndAsync();
                }
                catch (Exception)
                {//
                }
            }
            await Task.Delay(1000, ct);
        }
        throw new KnownException($"Error calling {url} : {lastError}");
    }
    
    public static async Task DownloadFile(this HttpClient client, string url, string path)
    {
        var response = await client.GetAsync(url);
        await using var fs = new FileStream(path, FileMode.Create);
        await response.Content.CopyToAsync(fs);
    }
    
    public static async Task<string> UploadImage(this HttpClient client, string url, string imagePath, string k)
    {
        var bytes = await File.ReadAllBytesAsync(imagePath);
        var content = new MultipartFormDataContent();
        var key = new StringContent(k);
        content.Add(new StreamContent(new MemoryStream(bytes)), "file", "upload.jpg");
        content.Add(key, "key");
        var response = await client.PostAsync(url, content);
        var s = await response.Content.ReadAsStringAsync();
        return s;
    }
    
    public static async Task<Response> PostJson(this HttpClient httpClient, string url, string json, int maxAttempts = 1, List<KeyValuePair<string, string>> headers = null, CancellationToken ct = new CancellationToken())
    {
        return await HandleAndRepeat(async () =>
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            if (content.Headers.ContentType != null)
                content.Headers.ContentType.CharSet = "";
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            req.TryAddHeaders(headers);
            var r = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            var s = await r.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var (h, cookies) = r.ParseHeaders();
            return new Response()
            {
                Text = WebUtility.HtmlDecode(s),
                Cookies = cookies,
                Headers = h
            };
        },url, ct,maxAttempts);
    }

    public static async Task<Response> PostFormData(this HttpClient httpClient, string url, List<KeyValuePair<string, string>> data, int maxAttempts = 1, List<KeyValuePair<string, string>> headers = null, CancellationToken ct = new CancellationToken())
    {
        return await HandleAndRepeat(async () =>
        {
            var content = new FormUrlEncodedContent(data);
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            req.TryAddHeaders(headers);
            var r = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            var s = await r.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var (h, cookies) = r.ParseHeaders();
            return new Response()
            {
                Text = WebUtility.HtmlDecode(s),
                Cookies = cookies,
                Headers = h
            };
        },url, ct,maxAttempts);
    }
    
     public static async Task<Response> Get(this HttpClient httpClient, string url, int maxAttempts = 1, List<KeyValuePair<string, string>> headers = null, CancellationToken ct = new CancellationToken())
     {
         return await HandleAndRepeat(async () =>
         {
             var req = new HttpRequestMessage(HttpMethod.Get, url);
             req.TryAddHeaders(headers);
             var r = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
             var s = await r.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
             var (h, cookies) = r.ParseHeaders();
             return new Response()
             {
                 Text = WebUtility.HtmlDecode(s),
                 Cookies = cookies,
                 Headers = h
             };
         },url, ct,maxAttempts);
     }
     

    public static async Task<HtmlDocument> ToDoc(this Task<string> task)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(await task.ConfigureAwait(false));
        return doc;
    }
    
    public static HtmlDocument ToDoc(this string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    public static List<KeyValuePair<string, string>> ToFormData(this string s)
    {
        var trs = s.Split('&');
        return trs
            .Select(tr => tr.Split('='))
            .Select(t => new KeyValuePair<string, string>(t[0], t[1])).ToList();
    }

    public static async Task<T> To<T>(this Task<string> task)
    {
        return JsonSerializer.Deserialize<T>(await task);
    }

    private static void TryAddHeaders(this HttpRequestMessage req,List<KeyValuePair<string,string>> headers)
    {
        req.Headers.TryAddWithoutValidation("Connection", "keep-alive");
        if (headers != null)
            foreach (var header in headers)
                req.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }
    private static (Dictionary<string,string> headers,Dictionary<string,string> cookies) ParseHeaders(this HttpResponseMessage resp)
    {
        var h = new Dictionary<string, string>();
        var cookies = new Dictionary<string, string>();
        foreach (var reqHeader in resp.Headers)
        {
            if (reqHeader.Key.ToLower() == "set-cookie")
            {
                var cc = reqHeader.Value.ParseCookieFromSet();
                foreach (var c in cc.Where(c => !cookies.ContainsKey(c.Key)))
                    cookies.Add(c.Key, c.Value);
                continue;
            }

            if (reqHeader.Key.StartsWith(":")) continue;
            if (reqHeader.Key.ToLower() == "content-length") continue;
            h.Add(reqHeader.Key, string.Join("\n", reqHeader.Value));
        }

        return (h, cookies);
    }

    public static async Task<string> MyIp(this HttpClient client)
    {
        return await client.GetStringAsync("https://api.ipify.org?format=json");
    }
}

public class Response
{
    public string Text { get; set; }
    public Dictionary<string,string> Headers { get; set; }
    public Dictionary<string,string> Cookies { get; set; }
}