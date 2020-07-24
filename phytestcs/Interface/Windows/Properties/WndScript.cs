using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using Object = phytestcs.Objects.Object;
using static phytestcs.Tools;

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

        [Flags]
        private enum PropType
        {
            Default = 1 << 0,
            Property = 1 << 1,
            Event = 1 << 2,
            Writable = 1 << 3,
            Computed = 1 << 4
        }
        
        public WndScript(Object obj, Vector2f pos)
            : base(obj, obj.Name, 440, pos)
        {
            var modes = new Dictionary<string, PropType>
            {
                [L["All"]] = PropType.Default,
                [L["Properties"]] = PropType.Property,
                [L["Computed properties"]] = PropType.Property | PropType.Computed,
                [L["Writable properties"]] = PropType.Property | PropType.Writable,
                [L["Events"]] = PropType.Event
            };

            var drop = new ComboBox();
            foreach (var mode in modes)
                drop.AddItem(mode.Key);
            
            Add(drop);

            var fields = new List<(TextFieldBase, PropType)>();

            foreach (var prop in obj
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name))
            {
                var ptype = PropType.Default;
                object? converter;
                var type = prop.PropertyType;
                object? propRef;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventWrapper<>))
                {
                    var ew = prop.GetValue(obj)!;
                    var lw = ew.GetType().GetProperty("Wrapper")!.GetValue(ew)!;
                    propRef = new PropertyReference<string>(lw.GetType().GetProperty("Code")!, lw);
                    type = typeof(string);
                    converter = null;
                    ptype |= PropType.Event;
                }
                else if (Converters.TryGetValue(type, out converter))
                {
                    propRef = Activator.CreateInstance(typeof(PropertyReference<>).MakeGenericType(type),
                        prop, obj);
                    ptype |= PropType.Property;
                }
                else
                    continue;

                if (prop.GetSetMethod() == null)
                    ptype |= PropType.Computed;
                else
                    ptype |= PropType.Writable;

                fields.Add(((TextFieldBase) Activator.CreateInstance(typeof(TextField<>).MakeGenericType(type), 
                    propRef, prop.Name, converter)!, ptype));
            }
            
            drop.ItemSelected += (sender, item) =>
            {
                _children.Clear();
                _height = 0;
                Container.RemoveAllWidgets();
                Add(drop);
                var type = modes[item.Item];
                var maxX = 0f;
                foreach (var (w, t) in fields)
                {
                    if ((t & type) == type)
                    {
                        w.NameLabel.AutoSize = true;
                        var width = w.NameWidth;
                        if (width > maxX)
                            maxX = width;
                        Add(w);
                    }
                }

                foreach (var (w, t) in fields)
                {
                    w.NameWidth = maxX;
                }
            };
            
            drop.SetSelectedItemByIndex(0);

            Show();
        }
    }
}