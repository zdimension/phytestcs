using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SFML.Graphics;
using SFML.System;
using TGUI;
using Object = phytestcs.Objects.Object;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndScript : WndBase<Object>
    {
        private static readonly Dictionary<Type, object> Converters = new Dictionary<Type, object>
        {
            [typeof(float)] = PropConverter.FloatString,
            [typeof(Vector2f)] = PropConverter.Vector2FString,
            [typeof(Color)] = PropConverter.ColorString,
            [typeof(HSVA)] = PropConverter.ColorHsvaString,
            [typeof(bool)] = PropConverter.BoolString
        };
        
        public WndScript(Object obj, Vector2f pos)
            : base(obj, obj.Name, 440, pos)
        {
            foreach (var prop in obj
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name))
            {
                object converter;
                var type = prop.PropertyType;

                if (!Converters.TryGetValue(type, out converter))
                    continue;

                var propRef = Activator.CreateInstance(typeof(PropertyReference<>).MakeGenericType(type),
                    prop, obj);

                Add((Widget) Activator.CreateInstance(typeof(TextField<>).MakeGenericType(type), propRef, prop.Name,
                    converter)!);
            }

            Show();
        }
    }
}