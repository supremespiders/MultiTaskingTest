using System.Net;

namespace MultiTaskingTest;

public class ScraperBase
{
    protected readonly HttpClient Client = new HttpClient(new HttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.All
    });
    
}