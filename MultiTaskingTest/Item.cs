using HtmlAgilityPack;
using MultiTaskingTest.Annotations;

namespace MultiTaskingTest;

public class Item
{
    public string Url { get; set; }
    [Xpath("//h1")]
    public string Title { get; set; }
    [Xpath("//table[@class='tab_retrait_mag']//tr")]
    public List<Availability> Availabilities { get; set; }

    public class Availability
    {
        [Xpath("./td[1]")]
        public string Location { get; set; }
        [Xpath("./td[2]")]
        public string Status { get; set; }
    }
}