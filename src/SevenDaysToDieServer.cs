using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Globalization;
using System.Net;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class SevenDaysServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get => GamePrefs.GetString(EnumGamePrefs.ServerName);
            set => GamePrefs.Set(EnumGamePrefs.ServerName, value);
        }

        private static IPAddress address;
        private static IPAddress localAddress;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                try
                {
                    if (address == null)
                    {
                        string serverIp = GamePrefs.GetString(EnumGamePrefs.ServerIP);
                        if (Utility.ValidateIPv4(serverIp) && !Utility.IsLocalIP(serverIp))
                        {
                            IPAddress.TryParse(serverIp, out address);
                            Interface.Oxide.LogInfo($"IP address from command-line: {address}");
                        }
                        else
                        {
                            WebClient webClient = new WebClient();
                            IPAddress.TryParse(webClient.DownloadString("http://api.ipify.org"), out address);
                            Interface.Oxide.LogInfo($"IP address from external API: {address}");
                        }
                    }

                    return address;
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server's public IP address", ex);
                    return IPAddress.Any;
                }
            }
        }

        /// <summary>
        /// Gets the local IP address of the server, if known
        /// </summary>
        public IPAddress LocalAddress
        {
            get
            {
                try
                {
                    return localAddress ?? (localAddress = Utility.GetLocalIP());
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server's local IP address", ex);
                    return IPAddress.Any;
                }
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => Convert.ToUInt16(GamePrefs.GetInt(EnumGamePrefs.ServerPort));

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => GamePrefs.GetString(EnumGamePrefs.GameVersion);

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => GameManager.Instance.World.Players.Count;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get => GamePrefs.GetInt(EnumGamePrefs.ServerMaxPlayerCount);
            set => GamePrefs.Set(EnumGamePrefs.ServerMaxPlayerCount, value);
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get
            {
                ulong time = GameManager.Instance.World.worldTime;
                return Convert.ToDateTime($"{GameUtils.WorldTimeToHours(time)}:{GameUtils.WorldTimeToMinutes(time)}");
            }
            set => GameUtils.DayTimeToWorldTime(value.Day, value.Hour, value.Minute);
        }

        /// <summary>
        /// Gets information on the currently loaded save file
        /// </summary>
        public SaveInfo SaveInfo => null;

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (!IsBanned(id))
            {
                // Ban player with reason
                GameManager.Instance.adminTools.AddBan(id, null, new DateTime(duration.Ticks), reason);

                // Kick player if connected
                if (IsConnected(id))
                {
                    Kick(id, reason);
                }
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id)
        {
            AdminToolsClientInfo adminClient = GameManager.Instance.adminTools.GetAdminToolsClientInfo(id);
            return adminClient.BannedUntil.TimeOfDay;
        }

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id)
        {
            return GameManager.Instance.adminTools.IsBanned(id);
        }

        /// <summary>
        /// Gets if the player is connected
        /// </summary>
        /// <param name="id"></param>
        public bool IsConnected(string id)
        {
            return ConnectionManager.Instance.GetClientInfoForPlayerId(id) != null;
        }

        /// <summary>
        /// Kicks the player for the specified reason
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        public void Kick(string id, string reason)
        {
            ClientInfo client = ConnectionManager.Instance.GetClientInfoForPlayerId(id);
            if (client != null)
            {
                GameUtils.KickPlayerData kickData = new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, DateTime.Now, reason);
                GameUtils.KickPlayerForClientInfo(client, kickData);
            }
        }

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            GameManager.Instance.SaveLocalPlayerData();
            GameManager.Instance.SaveWorld();
        }

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (IsBanned(id))
            {
                // Set to unbanned
                GameManager.Instance.adminTools.RemoveBan(id);
            }
        }

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts the specified chat message and prefix to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix, params object[] args)
        {
            message = args.Length > 0 ? string.Format(Formatter.ToRoKAnd7DTD(message), args) : Formatter.ToRoKAnd7DTD(message);
            string formatted = prefix != null ? $"{prefix} {message}" : message;

            GameManager.Instance.GameMessageServer(null, EnumGameMessages.Chat, formatted, null, false, null, false);
        }

        /// <summary>
        /// Broadcasts the specified chat message to all players
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => Broadcast(message, null);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            SdtdConsole.Instance.ExecuteSync($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", null);
        }

        #endregion Chat and Commands
    }
}
