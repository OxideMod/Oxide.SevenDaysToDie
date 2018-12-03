using uMod.Libraries.Universal;

namespace uMod.SevenDaysToDie
{
    /// <summary>
    /// Provides universal functionality for the game "7 Days to Die"
    /// </summary>
    public class SevenDaysToDieProvider : IUniversalProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "7 Days to Die";

        /// <summary>
        /// Gets the Steam app ID of the game's client, if available
        /// </summary>
        public uint ClientAppId => 251570;

        /// <summary>
        /// Gets the Steam app ID of the game's server, if available
        /// </summary>
        public uint ServerAppId => 294420;

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static SevenDaysToDieProvider Instance { get; private set; }

        public SevenDaysToDieProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public SevenDaysToDiePlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public SevenDaysCommandSystem CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new SevenDaysToDieServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager()
        {
            PlayerManager = new SevenDaysToDiePlayerManager();
            PlayerManager.Initialize();
            return PlayerManager;
        }

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new SevenDaysCommandSystem();

        /// <summary>
        /// Formats the text with markup as specified in uMod.Libraries.Formatter
        /// into the game-specific markup language
        /// </summary>
        /// <param name="text">text to format</param>
        /// <returns>formatted text</returns>
        public string FormatText(string text) => Formatter.ToRoKAnd7DTD(text);
    }
}
