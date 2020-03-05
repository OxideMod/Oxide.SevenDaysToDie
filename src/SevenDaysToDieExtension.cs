using Oxide.Core;
using Oxide.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class SevenDaysExtension : Extension
    {
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;

        /// <summary>
        /// Gets whether this extension is for a specific game
        /// </summary>
        public override bool IsGameExtension => true;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "SevenDaysToDie";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

        /// <summary>
        /// Gets the branch of this extension
        /// </summary>
        public override string Branch => "public"; // TODO: Handle this programmatically

        /// <summary>
        /// Commands that plugins can't override
        /// </summary>
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        /// <summary>
        /// List of default game-specific references for use in plugins
        /// </summary>
        public override string[] DefaultReferences => new[]
        {
            "UnityEngine.AIModule", "UnityEngine.AssetBundleModule", "UnityEngine.CoreModule", "UnityEngine.GridModule", "UnityEngine.ImageConversionModule",
            "UnityEngine.Networking", "UnityEngine.PhysicsModule", "UnityEngine.TerrainModule", "UnityEngine.TerrainPhysicsModule", "UnityEngine.UI", "UnityEngine.UIModule",
            "UnityEngine.UIElementsModule", "UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestModule", "UnityEngine.UnityWebRequestTextureModule",
            "UnityEngine.UnityWebRequestWWWModule", "UnityEngine.VehiclesModule", "UnityEngine.WebModule"
        };

        /// <summary>
        /// List of assemblies allowed for use in plugins
        /// </summary>
        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine"
        };

        /// <summary>
        /// List of namespaces allowed for use in plugins
        /// </summary>
        public override string[] WhitelistNamespaces => new[]
        {
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        /// <summary>
        /// List of filter matches to apply to console output
        /// </summary>
        public static string[] Filter =
        {
            "* SKY INITIALIZED",
            "Awake done",
            "Biomes image size",
            "Command line arguments:",
            "Dedicated server only build",
            "Exited thread thread_",
            "GamePref.",
            "GameStat.",
            "HDR Render",
            "HDR and MultisampleAntiAliasing",
            "INF AIDirector:",
            "INF Adding observed entity:",
            "INF BiomeSpawnManager spawned",
            "INF Cleanup",
            "INF Clearing all pools",
            "INF Created player with",
            "INF Disconnect",
            "INF GMA.",
            "INF GOM.",
            "INF Kicking player:",
            "INF OnApplicationQuit",
            "INF PPS RequestToEnterGame sending player list",
            "INF Removing observed entity",
            "INF RequestToEnterGame:",
            "INF RequestToSpawnPlayer:",
            "INF Spawned",
            "INF Start a new wave",
            "INF Time:",
            "INF Token length:",
            "INF WSD.",
            "Load key config",
            "Loading permissions file at",
            "NET: Starting server protocols",
            "NET: Stopping server protocols",
            "NET: Unity NW server",
            "POI image size",
            "Parsing server configfile:",
            "Persistent GamePrefs saved",
            "SaveAndCleanupWorld",
            "SelectionBoxManager.Instance:",
            "Setting breakpad minidump AppID",
            "StartAsServer",
            "StartGame",
            "Started thread",
            "WRN ApplyAllowControllerOption",
            "Weather Packages Created",
            "World.Cleanup",
            "World.Load:",
            "World.Unload",
            "WorldStaticData.Init()",
            "[EAC] FreeUser",
            "[EAC] Log:",
            "[EAC] UserStatusHandler callback",
            "[NET] PlayerConnected",
            "[NET] PlayerDisconnected",
            "[NET] ServerShutdown",
            "[Steamworks.NET]",
            "createWorld() done",
            "createWorld:"
        };

        /// <summary>
        /// Initializes a new instance of the SevenDaysExtension class
        /// </summary>
        /// <param name="manager"></param>
        public SevenDaysExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new SevenDaysPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
        }
    }
}
