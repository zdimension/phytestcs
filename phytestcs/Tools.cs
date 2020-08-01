using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace phytestcs
{
    public static partial class Tools
    {
        private const float DefaultObjectSizeFactor = 68.3366809f;
        public static readonly Random Rng = new Random();

        public static readonly IStringLocalizer L2 = new ResourceManagerStringLocalizerFactory(
            new OptionsWrapper<LocalizationOptions>(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance).Create(typeof(Tools));

        public static LTest L = new LTest();

        public static float DefaultObjectSize => DefaultObjectSizeFactor / Camera.Zoom;
        public static float DefaultSpringSize => DefaultObjectSize * 0.4f;

        public sealed class LTest
        {
            private readonly Dictionary<string, string> _cache = new Dictionary<string,string>();
            public string this[string name]
            {
                get
                {
                    var s = (string?) typeof(ResourceManagerStringLocalizer)
                            .GetMethod("GetStringSafely", BindingFlags.NonPublic | BindingFlags.Instance)!
                        .Invoke(L2, new object[] { name, new CultureInfo("en") });
                    if (false && s == null)
                        Console.WriteLine($@"    <data name=""{name}"" xml:space=""preserve"">
        <value>{name}</value>
    </data>");
                    if (_cache.TryGetValue(name, out var val))
                        return val;
                    return _cache[name] = s ?? name;
                }
            }
        }
    }
}