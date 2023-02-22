namespace MultiTaskingTest.Annotations;
[AttributeUsage(AttributeTargets.Property)]
public class XpathAttribute: Attribute
{
    private readonly string[] _xPaths;

    public List<string> XPaths()
    {
        return _xPaths.ToList();
    }

    public XpathAttribute(params string[] xPaths)
    {
        _xPaths = xPaths;
    }
}