using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using phytestcs.Interface;

namespace phytestcs
{
    public abstract class ToStringProxy
    {
        public override string ToString()
        {
            return this.Repr();
        }
    }

    public static partial class Tools
    {
        public static void Log(params object?[] obj)
        {
            foreach(var o in obj)
                Console.WriteLine(o.Repr());
        }
        
        public static string Repr(this ITuple tuple)
        {
            if (tuple == null) throw new ArgumentNullException(nameof(tuple));
            return $"({string.Join(", ", Enumerable.Range(0, tuple.Length).Select(x => Repr(tuple[x])))})";
        }
        
        public static string Repr(this object? o, int maxDepth=5, int currentDepth=0)
        {
            if (o == null)
                return "null";

            if (typeof(Tools).GetMethod("Repr", new[] { o.GetType() }) is {} m && m.GetParameters()[0].ParameterType != typeof(object))
                return (string) m.Invoke(null, new[] { o })!;
				
            switch (o)
            {
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture)!;
                case ITuple t:
                    return t.Repr();
            }

            if (o.GetType().GetMethod("ToString", Array.Empty<Type>()) is {} m2 && m2.DeclaringType != typeof(object))
                return (string) m2.Invoke(o, System.Array.Empty<object>())!;

            var type = o.GetType();
            var res = new StringBuilder();
            res.Append(type.Name + " {");
            if (maxDepth > currentDepth)
            {
                res.Append(" ... }");
            }
            else
            {
                res.AppendJoin(',', type.GetProperties().Select(prop =>
                {
                    var r = ($"\n\t{prop.Name} = ");
                    var val = prop.GetValue(o);
                    var str = Repr(val, maxDepth, currentDepth + 1);
                    var lines = str.Split('\n');
                    r += (lines[0]);
                    for (var i = 1; i < lines.Length; i++)
                        r += ("\n\t" + lines[i]);

                    return r;
                }));
                res.Append("\n}");
            }

            return res.ToString();
        }
    }
}