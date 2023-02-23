using HtmlAgilityPack;
using MultiTaskingTest.Extensions;

namespace MultiTaskingTest;

public class XpathOptimizer
{
    private List<string> _chosenPaths=new();
    private readonly List<HtmlNode> _checkedNodes=new();
    private HtmlDocument _doc;

    private int XpathLength(string s)
    {
        var x=s.Replace("//", "/").Split("/").Length;
        return x;
    }
    
    public void Optimize(string xpath, HtmlDocument doc)
    {
        _doc = doc;
        var targetNodes = doc.DocumentNode.SelectNodes(xpath);
        if (targetNodes is not { Count: 1 }) throw new KnownException("Target node not right");
        CheckNode(targetNodes.First(), "");
        Console.WriteLine("Result : ");
        var text = targetNodes.First().InnerText;
        _chosenPaths = _chosenPaths.Where(x => XpathLength(x) <= 4 && !x.Contains(text)).ToList();
        _chosenPaths=_chosenPaths.OrderBy(x => x.Length).ToList();
        
        foreach (var chosenPath in _chosenPaths)
        {
            Console.WriteLine(chosenPath);
        }
    }
    
     List<string> GetPerfectPaths(HtmlNode node)
        {
            var nodePath = $"//{node.Name}";
            var optimalPaths = new List<string>();
            foreach (var nodeAttribute in node.Attributes)
            {
                //if (!IsEnglish(nodeAttribute.Value)) continue;
                //if (nodeAttribute.Name == "class") continue;
                var xpath = nodePath + $"[@{nodeAttribute.Name}='{nodeAttribute.Value}']";
                var resultCount = _doc.DocumentNode.SelectNodes(xpath).Count;
                if (resultCount != 1) continue;
                optimalPaths.Add(xpath);
            }

            if (_doc.DocumentNode.SelectNodes(nodePath).Count == 1)
                optimalPaths.Add(nodePath);

            var text = node.InnerText;
            if (text.Length < 50 && _doc.DocumentNode.SelectNodes($"{nodePath}[text()='{text}']")!=null)
            {
                if (_doc.DocumentNode.SelectNodes($"{nodePath}[text()='{text}']").Count == 1)
                    optimalPaths.Add($"{nodePath}[text()='{text}']");
            }
            return optimalPaths;
        }
        void CheckNode(HtmlNode node, string p)
        {
            if (_checkedNodes.Contains(node)) return;
            _checkedNodes.Add(node);
            var perfectPaths = GetPerfectPaths(node);
            foreach (var path in perfectPaths)
            {
                _chosenPaths.Add($"{path}{p}");
            }

            // if (!p.StartsWith("/following-sibling"))
            CheckPreviousNode(node, p);

            if (node.Name == "html") return;
            //parent
            var (position, total) = node.Position();
            var c = $"/{node.Name}{(total == 1 ? "" : $"[{position}]")}{p}";
            CheckNode(node.ParentNode, c);
        }

        void CheckPreviousNode(HtmlNode node, string p)
        {
            var previousSibling = GetPreviousNonTextNode(node);
            if (previousSibling == null) return;
            var c = $"/following-sibling::{node.Name}[1]{p}";
            CheckNode(previousSibling, c);
        }

        HtmlNode GetPreviousNonTextNode(HtmlNode node)
        {
            var prev = node;
            do
            {
                prev = prev.PreviousSibling;
                if (prev == null) return null;
                if (!prev.Name.StartsWith("#")) return prev;
            } while (true);
        }
}