using System;
using uMod.Plugins;

namespace uMod.SevenDaysToDie
{
    /// <summary>
    /// Responsible for loading the core 7 Days To Die plugin
    /// </summary>
    public class SevenDaysPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(SevenDaysToDie) };
    }
}
