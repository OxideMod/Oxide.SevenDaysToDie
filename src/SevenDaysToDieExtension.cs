using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uMod.Extensions;
using uMod.Unity;
using UnityEngine;

namespace uMod.SevenDaysToDie
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class SevenDaysExtension : Extension
    {
        // Get assembly info
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
            ""
        };

        /// <summary>
        /// List of assemblies allowed for use in plugins
        /// </summary>
        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "uMod", "System", "System.Core", "UnityEngine"
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
            if (Interface.uMod.EnableConsole())
            {
                Application.logMessageReceivedThreaded += HandleLog;

                Interface.uMod.ServerConsole.Input += ServerConsoleOnInput;
                Interface.uMod.ServerConsole.Completion = input =>
                {
                    return string.IsNullOrEmpty(input) ? null : SdtdConsole.Instance.GetCommands().SelectMany(g => g.GetCommands())
                            .Where(c => c.StartsWith(input.ToLower())).ToArray();
                };
            }
        }

        internal static void ServerConsole()
        {
            if (Interface.uMod.ServerConsole == null)
            {
                return;
            }

            Interface.uMod.ServerConsole.Title = () => $"{GameManager.Instance.World.Players.Count} | {GamePrefs.GetString(EnumGamePrefs.ServerName)}";

            Interface.uMod.ServerConsole.Status1Left = () => GamePrefs.GetString(EnumGamePrefs.ServerName);
            Interface.uMod.ServerConsole.Status1Right = () =>
            {
                TimeSpan time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                string uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.uMod.ServerConsole.Status2Left = () =>
            {
                string players = $"{GameManager.Instance.World.Players.Count}/{GamePrefs.GetInt(EnumGamePrefs.ServerMaxPlayerCount)}";
                int entities = GameManager.Instance.World.Entities.Count;
                return $"{players}, {entities + (entities.Equals(1) ? " entity" : " entities")}";
            };
            Interface.uMod.ServerConsole.Status2Right = () => string.Empty; // TODO: Network in/out

            Interface.uMod.ServerConsole.Status3Left = () =>
            {
                ulong gameTime = GameManager.Instance.World.worldTime;
                string dateTime = Convert.ToDateTime($"{GameUtils.WorldTimeToHours(gameTime)}:{GameUtils.WorldTimeToMinutes(gameTime)}").ToString("h:mm tt");
                return $"{dateTime.ToLower()}, {GamePrefs.GetString(EnumGamePrefs.GameWorld)} [{GamePrefs.GetString(EnumGamePrefs.GameName)}]";
            };
            Interface.uMod.ServerConsole.Status3Right = () => $"uMod.SevenDaysToDie {AssemblyVersion}";
            Interface.uMod.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            input = input.Trim();
            List<string> result = SdtdConsole.Instance.ExecuteSync(input, null);
            if (result != null)
            {
                Interface.uMod.ServerConsole.AddMessage(string.Join("\n", result.ToArray()));
            }
        }

        private static void HandleLog(string message, string stackTrace, LogType logType)
        {
            if (!string.IsNullOrEmpty(message) && !Filter.Any(message.Contains))
            {
                Interface.uMod.RootLogger.HandleMessage(message, stackTrace, logType.ToLogType());
            }
        }
    }
}
