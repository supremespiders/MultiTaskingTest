using MultiTaskingTest.Annotations;
using MultiTaskingTest.Extensions;

namespace MultiTaskingTest;

public class MyTekScraper:ScraperBase
{
    public async Task Main()
    {
        var response = await Client.Get("https://www.mytek.tn/cable-lenyes-926-2-4a-micro-usb-noir.html");
        var item=response.Text.Parse<Item>();
    }
}