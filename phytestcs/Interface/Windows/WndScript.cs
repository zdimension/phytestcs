using System;
using System.Linq;
using System.Reflection;
using SFML.System;
using TGUI;
using Object = phytestcs.Objects.Object;

namespace phytestcs.Interface.Windows
{
    public class WndScript : WndBase<Object>
    {
        public WndScript(Object obj, Vector2f pos)
            : base(obj, obj.Name, 440, pos)
        {
            foreach (var prop in obj
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name))
            {
                object converter = null;
                var type = prop.PropertyType;


                if (type == typeof(float))
                {
                    converter = PropConverter.FloatString;
                }
                else if (type == typeof(Vector2f))
                {
                    converter = PropConverter.Vector2fString;
                }
                else if (type == typeof(bool))
                {
                    converter = PropConverter.EvalString<bool>();
                }
                else
                {
                    continue;
                }

                var propRef = Activator.CreateInstance(typeof(PropertyReference<>).MakeGenericType(type),
                    prop, obj);

                Add((Widget) Activator.CreateInstance(typeof(TextField<>).MakeGenericType(type), propRef, prop.Name,
                    converter));
            }

            Show();
        }
    }
}