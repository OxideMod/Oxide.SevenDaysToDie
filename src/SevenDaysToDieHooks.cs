using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Game hooks and wrappers for the core 7 Days to Die plugin
    /// </summary>
    public partial class SevenDaysCore
    {
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
                IPlayer iplayer = client.IPlayer;
                object chatSpecific = Interface.Call("OnPlayerChat", client, message);
                object chatCovalence = iplayer != null ? Interface.Call("OnUserChat", iplayer, message) : null;
                return chatSpecific ?? chatCovalence;
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

            // Call out and see if we should reject
            object canLogin = Interface.Call("CanClientLogin", client) ?? Interface.Call("CanUserLogin", client.playerName, client.playerId, client.ip); // TODO: Localization

            if (canLogin is string || canLogin is bool && !(bool)canLogin)
            {
                string reason = canLogin is string ? canLogin.ToString() : "Connection was rejected"; // TODO: Localization
                GameUtils.KickPlayerData kickData = new GameUtils.KickPlayerData(GameUtils.EKickReason.PlayerLimitExceeded, 0, default(DateTime), reason);
                GameUtils.KickPlayerForClientInfo(client, kickData);
                return true;
            }

            // Call game and covalence hooks
            return Interface.Call("OnUserApprove", client) ?? Interface.Call("OnUserApproved", client.playerName, client.playerId, client.ip); // TODO: Localization
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(ClientInfo client)
        {
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
            IPlayer iplayer = Covalence.PlayerManager.FindPlayerById(client.playerId);
            if (iplayer != null)
            {
                client.IPlayer = iplayer;
                Interface.Call("OnUserConnected", iplayer);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ClientInfo client)
        {
            IPlayer iplayer = client.IPlayer;
            if (iplayer != null)
            {
                // Call covalence hook
                Interface.Call("OnUserDisconnected", iplayer, "Unknown");
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
            IPlayer iplayer = client.IPlayer;
            if (iplayer != null)
            {
                // Call covalence hook
                Interface.Call("OnUserSpawn", iplayer);
            }
        }

        #endregion Player Hooks
    }
}
