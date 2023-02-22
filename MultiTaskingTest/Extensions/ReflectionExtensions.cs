using System.Linq.Expressions;

namespace MultiTaskingTest.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly Dictionary<Type, Dictionary<string, Delegate>> GetterCache = new();
        private static readonly Dictionary<Type, Dictionary<string, Delegate>> SetterCache = new();

        public static Func<T, T2> Getter<T, T2>(this T myClass, Expression<Func<T, T2>> selector)
        {
            return (Func<T, T2>)myClass.FromCache<T, T2>(selector.GetPropertyName(), true);
        }

        public static Func<T, T2> Getter<T, T2>(this T myClass, string propertyName)
        {
            return (Func<T, T2>)myClass.FromCache<T, T2>(propertyName, true);
        }

        public static Action<T, T2> Setter<T, T2>(this T myClass, Expression<Func<T, T2>> selector)
        {
            return (Action<T, T2>)myClass.FromCache<T, T2>(selector.GetPropertyName(), false);
        }

        public static Action<T, T2> Setter<T, T2>(this T myClass, string propertyName)
        {
            return (Action<T, T2>)myClass.FromCache<T, T2>(propertyName, false);
        }

        public static string GetPropertyName(this LambdaExpression lambdaExpression)
        {
            if (!(lambdaExpression.Body is MemberExpression x)) throw new Exception("null selector");
            return x.Member.Name;
        }

        private static Delegate FromCache<T, T2>(this T myClass, string propertyName, bool getter)
        {
            var classType = myClass.GetType();
            var cache = getter ? GetterCache : SetterCache;
            if (cache.ContainsKey(classType) && cache[classType].ContainsKey(propertyName))
                return cache[classType][propertyName];
            var methodInfo = getter ? classType.GetProperty(propertyName)?.GetGetMethod() : classType.GetProperty(propertyName)?.GetSetMethod();
            if (methodInfo == null) throw new Exception("Null methodInfo");
            var myDelegate = getter
                ? Delegate.CreateDelegate(typeof(Func<T, T2>), methodInfo)
                : Delegate.CreateDelegate(typeof(Action<T, T2>), methodInfo);

            if (!cache.ContainsKey(classType))
                cache.Add(classType, new Dictionary<string, Delegate>());
            cache[classType].Add(propertyName, myDelegate);
            return myDelegate;
        }
    }
}