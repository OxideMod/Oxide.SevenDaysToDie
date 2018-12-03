using System;
using uMod.Configuration;
using uMod.Libraries.Universal;
using uMod.Plugins;

namespace uMod.SevenDaysToDie
{
    /// <summary>
    /// Game hooks and wrappers for the core 7 Days to Die plugin
    /// </summary>
    public partial class SevenDaysToDie
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
                // Let plugins know
                IPlayer player = client.IPlayer;
                object chatSpecific = Interface.Call("OnPlayerChat", client, message);
                object chatUniversal = player != null ? Interface.Call("OnPlayerChat", player, message) : null;
                return chatSpecific ?? chatUniversal;
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
            // Let universal know
            Universal.PlayerManager.PlayerJoin(client.playerId, client.playerName); // TODO: Handle this automatically

            // Call universal hook
            object canLogin = Interface.Call("CanPlayerLogin", client.playerName, client.playerId, client.ip);
            if (canLogin is string || canLogin is bool && !(bool)canLogin)
            {
                // Reject player with message
                string reason = canLogin is string ? canLogin.ToString() : "Connection was rejected"; // TODO: Localization
                GameUtils.KickPlayerData kickData = new GameUtils.KickPlayerData(GameUtils.EKickReason.PlayerLimitExceeded, 0, default(DateTime), reason);
                GameUtils.KickPlayerForClientInfo(client, kickData);
                return true;
            }

            // Let plugins know
            Interface.Call("OnPlayerApproved", client.playerName, client.playerId, client.ip);

            return null;
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
                // Update player's stored username
                permission.UpdateNickname(client.playerId, client.playerName);

                // Set default groups, if necessary
                uModConfig.DefaultGroups defaultGroups = Interface.uMod.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(client.playerId, defaultGroups.Players))
                {
                    permission.AddUserGroup(client.playerId, defaultGroups.Players);
                }
                if (GameManager.Instance.adminTools.IsAdmin(client.playerId) && !permission.UserHasGroup(client.playerId, defaultGroups.Administrators))
                {
                    permission.AddUserGroup(client.playerId, defaultGroups.Administrators);
                }
            }

            // Let universal know
            Universal.PlayerManager.PlayerConnected(client);

            IPlayer player = Universal.PlayerManager.FindPlayerById(client.playerId);
            if (player != null)
            {
                // Set IPlayer object on ClientInfo
                client.IPlayer = player;

                // Call universal hook
                Interface.Call("OnPlayerConnected", player);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ClientInfo client)
        {
            // Let universal know
            Universal.PlayerManager.PlayerDisconnected(client);

            IPlayer player = client.IPlayer;
            if (player != null)
            {
                // Call universal hook
                Interface.Call("OnPlayerDisconnected", player, "Unknown");
            }
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
                // Call universal hook
                Interface.Call("OnPlayerSpawn", player);
            }
        }

        #endregion Player Hooks
    }
}
