using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace phytestcs
{
    public static class Global
    {
        public static readonly Random RNG = new Random();

        public static readonly IStringLocalizer L = new ResourceManagerStringLocalizerFactory(
            new OptionsWrapper<LocalizationOptions>(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance).Create(typeof(Global));
    }
}
