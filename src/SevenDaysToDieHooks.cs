using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

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
        /// <param name="client"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(ClientInfo client, string message)
        {
            if (client != null && !string.IsNullOrEmpty(message))
            {
                // Check if it is a chat command
                if (message[0] == '/')
                {
                    Covalence.CommandSystem.ParseCommand(message.TrimStart('/'), out string cmd, out string[] args);
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        // Is the command blocked?
                        object commandSpecific = Interface.CallHook("OnPlayerCommand", client, cmd, args);
                        object commandCovalence = Interface.CallHook("OnUserCommand", client.IPlayer, cmd, args);
                        object canBlock = commandSpecific is null ? commandCovalence : commandSpecific;
                        if (canBlock != null)
                        {
                            return true;
                        }

                        // Is it a valid chat command?
                        if (!Covalence.CommandSystem.HandleChatMessage(client.IPlayer, message))
                        {
                            client.IPlayer.Reply(string.Format(lang.GetMessage("UnknownCommand", this, client.IPlayer.Id), cmd));
                        }
                    }

                    return true;
                }

                // Call hooks for plugins
                object chatSpecific = Interface.Call("OnPlayerChat", client, message);
                object chatCovalence = Interface.Call("OnUserChat", client.IPlayer, message);
                return chatSpecific is null ? chatCovalence : chatSpecific;
            }

            return null;
        }

        /// <summary>
        /// Called when the player is attempting to connect
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(ClientInfo client)
        {
            // Let covalence know
            Covalence.PlayerManager.PlayerJoin(client.playerId, client.playerName);

            // Call hooks for plugins
            object loginSpecific = Interface.Call("CanClientLogin", client);
            object loginCovalence = Interface.Call("CanUserLogin", client.playerName, client.playerId, client.ip);
            object canLogin = loginSpecific is null ? loginCovalence : loginSpecific;
            if (canLogin is string || canLogin is bool loginBlocked && !loginBlocked)
            {
                string reason = canLogin is string ? canLogin.ToString() : "Connection was rejected"; // TODO: Localization
                GameUtils.KickPlayerData kickData = new GameUtils.KickPlayerData(GameUtils.EKickReason.PlayerLimitExceeded, 0, default, reason);
                GameUtils.KickPlayerForClientInfo(client, kickData);
                return true;
            }

            // Call hooks for plugins
            object approvedSpecific = Interface.Call("OnUserApprove", client);
            object approvedCovalence = Interface.Call("OnUserApproved", client.playerName, client.playerId, client.ip);
            return approvedSpecific is null ? approvedCovalence : approvedSpecific;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(ClientInfo client)
        {
            // Update name and groups with permissions
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(client.playerId, client.playerName);
                OxideConfig.DefaultGroups defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(client.playerId, defaultGroups.Players))
                {
                    permission.AddUserGroup(client.playerId, defaultGroups.Players);
                }
                if (GameManager.Instance.adminTools.IsAdmin(client.playerId) && !permission.UserHasGroup(client.playerId, defaultGroups.Administrators))
                {
                    permission.AddUserGroup(client.playerId, defaultGroups.Administrators);
                }
            }

            // Let covalence know
            Covalence.PlayerManager.PlayerConnected(client);

            IPlayer player = Covalence.PlayerManager.FindPlayerById(client.playerId);
            if (player != null)
            {
                client.IPlayer = player;

                // Call hooks for plugins
                Interface.Call("OnUserConnected", player);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ClientInfo client)
        {
            IPlayer player = client.IPlayer;
            if (player != null)
            {
                // Call hooks for plugins
                Interface.Call("OnUserDisconnected", player, "Unknown"); // TODO: Localization
            }

            // Let covalence know
            Covalence.PlayerManager.PlayerDisconnected(client);
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(ClientInfo client)
        {
            IPlayer player = client.IPlayer;
            if (player != null)
            {
                // Call hooks for plugins
                Interface.Call("OnUserSpawn", player);
            }
        }

        #endregion Player Hooks
    }
}
