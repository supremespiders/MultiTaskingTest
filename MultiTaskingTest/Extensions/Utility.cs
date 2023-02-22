using System.Collections;
using HtmlAgilityPack;
using MultiTaskingTest.Annotations;

namespace MultiTaskingTest.Extensions;

public static class Utility
{
   public static Dictionary<string, List<string>> GetPropertiesXpath(this Type t)
   {
      var properties = t.GetProperties();
      var dic = new Dictionary<string, List<string>>();
      foreach (var property in properties)
      {
         var x = (XpathAttribute)property.GetCustomAttributes(typeof(XpathAttribute),false).FirstOrDefault();
         if(x==null) continue;
         dic.Add(property.Name,x.XPaths());
      }
      return dic;
   }

   public static async Task<T> Parse<T>(this Task<string> t) where T : new()
   {
      return (await t).Parse<T>();
   }
   public static T Parse<T>(this string t) where T : new()
   {
      var doc = new HtmlDocument();
      doc.LoadHtml(t);
      var pX = typeof(T).GetPropertiesXpath();
      var obj = new T();
      foreach (var p in pX)
      {
         var pr = typeof(T).GetProperty(p.Key);
         if (pr.PropertyType.IsGenericType && pr.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
         {
            var of = pr.PropertyType.GetGenericArguments()[0];

            var px2 = of.GetPropertiesXpath();
            var nodes = doc.DocumentNode.SelectNodes(p.Value[0]);
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(of));
            foreach (var node in nodes)
            {
               var one = Activator.CreateInstance(of);
               foreach (var px in px2)
               {
                  var v = node.SelectSingleNode(px.Value[0])?.InnerText;
                  var e = of.GetProperty(px.Key);
                  e.SetValue(one,v);
               }
               list.Add(one);
            }
            pr.SetValue(obj,list);
         }
         else
         {
            var val = doc.DocumentNode.SelectSingleNode(p.Value[0])?.InnerText;
            var setter = obj.Setter<T,string>(p.Key);
            setter(obj, val);
         }
      }
      return obj;
   }
}