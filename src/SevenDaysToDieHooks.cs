using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Platform.Steam;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Game hooks and wrappers for the core 7 Days to Die plugin
    /// </summary>
    public partial class SevenDaysCore
    {
        #region Server Hooks

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HookMethod("IOnServerCommand")]
        private object IOnServerCommand(string command)
        {
            command = command.TrimStart('/').Trim();

            if (string.IsNullOrEmpty(command) || command.Length == 0)
            {
                return null;
            }

            // Parse the command
            Covalence.CommandSystem.ParseCommand(command, out string cmd, out string[] args);
            if (string.IsNullOrEmpty(cmd))
            {
                return null;
            }

            // Is the command blocked?
            if (Interface.Call("OnServerCommand", cmd, args) != null)
            {
                return true;
            }

            return null;
        }

        #endregion Server Hooks

        #region Player Hooks

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(ClientInfo clientInfo, string message)
        {
            if (clientInfo != null && !string.IsNullOrEmpty(message))
            {
                // Check if it is a chat command
                if (message[0] == '/')
                {
                    Covalence.CommandSystem.ParseCommand(message.TrimStart('/'), out string cmd, out string[] args);
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        // Is the command blocked?
                        object commandSpecific = Interface.CallHook("OnPlayerCommand", clientInfo, cmd, args);
                        object commandCovalence = Interface.CallHook("OnUserCommand", clientInfo.IPlayer, cmd, args);
                        object canBlock = commandSpecific is null ? commandCovalence : commandSpecific;
                        if (canBlock != null)
                        {
                            return true;
                        }

                        // Is it a valid chat command?
                        if (!Covalence.CommandSystem.HandleChatMessage(clientInfo.IPlayer, message))
                        {
                            clientInfo.IPlayer.Reply(string.Format(lang.GetMessage("UnknownCommand", this, clientInfo.IPlayer.Id), cmd));
                        }
                    }

                    return true;
                }

                // Call hooks for plugins
                object chatSpecific = Interface.Call("OnPlayerChat", clientInfo, message);
                object chatCovalence = Interface.Call("OnUserChat", clientInfo.IPlayer, message);
                return chatSpecific is null ? chatCovalence : chatSpecific;
            }

            return null;
        }

        /// <summary>
        /// Called when the player is attempting to connect
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(ClientInfo clientInfo)
        {
            string playerId = ((UserIdentifierSteam)clientInfo.PlatformId).ReadablePlatformUserIdentifier;

            // Let covalence know
            Covalence.PlayerManager.PlayerJoin(playerId, clientInfo.playerName);

            // Call hooks for plugins
            object loginSpecific = Interface.Call("CanClientLogin", clientInfo);
            object loginCovalence = Interface.Call("CanUserLogin", clientInfo.playerName, playerId, clientInfo.ip);
            object canLogin = loginSpecific is null ? loginCovalence : loginSpecific;
            if (canLogin is string || canLogin is bool loginBlocked && !loginBlocked)
            {
                string reason = canLogin is string ? canLogin.ToString() : "Connection was rejected"; // TODO: Localization
                GameUtils.KickPlayerData kickData = new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, reason);
                GameUtils.KickPlayerForClientInfo(clientInfo, kickData);
                return true;
            }

            // Call hooks for plugins
            object approvedSpecific = Interface.Call("OnUserApprove", clientInfo);
            object approvedCovalence = Interface.Call("OnUserApproved", clientInfo.playerName, playerId, clientInfo.ip);
            return approvedSpecific is null ? approvedCovalence : approvedSpecific;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="clientInfo"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(ClientInfo clientInfo)
        {
            string playerId = ((UserIdentifierSteam)clientInfo.PlatformId).ReadablePlatformUserIdentifier;

            // Update name and groups with permissions
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(playerId, clientInfo.playerName);
                OxideConfig.DefaultGroups defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(playerId, defaultGroups.Players))
                {
                    permission.AddUserGroup(playerId, defaultGroups.Players);
                }
                if (GameManager.Instance.adminTools.Users.HasEntry(clientInfo) && !permission.UserHasGroup(playerId, defaultGroups.Administrators))
                {
                    permission.AddUserGroup(playerId, defaultGroups.Administrators);
                }
            }

            // Let covalence know
            Covalence.PlayerManager.PlayerConnected(clientInfo);

            IPlayer player = Covalence.PlayerManager.FindPlayerById(playerId);
            if (player != null)
            {
                clientInfo.IPlayer = player;

                // Call hooks for plugins
                Interface.Call("OnUserConnected", player);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="clientInfo"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ClientInfo clientInfo)
        {
            IPlayer player = clientInfo.IPlayer;
            if (player != null)
            {
                // Call hooks for plugins
                Interface.Call("OnUserDisconnected", player, "Unknown"); // TODO: Localization
            }

            // Let covalence know
            Covalence.PlayerManager.PlayerDisconnected(clientInfo);
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="clientInfo"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(ClientInfo clientInfo)
        {
            IPlayer player = clientInfo.IPlayer;
            if (player != null)
            {
                // Call hooks for plugins
                Interface.Call("OnUserSpawn", player);
            }
        }

        #endregion Player Hooks
    }
}
