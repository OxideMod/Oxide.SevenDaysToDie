﻿extern alias References;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Platform.Steam;
using References::ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class SevenDaysPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, SevenDaysPlayer> allPlayers;
        private IDictionary<string, SevenDaysPlayer> connectedPlayers;

        internal void Initialize()
        {
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, SevenDaysPlayer>();
            connectedPlayers = new Dictionary<string, SevenDaysPlayer>();

            foreach (KeyValuePair<string, PlayerRecord> pair in playerData)
            {
                allPlayers.Add(pair.Key, new SevenDaysPlayer(pair.Value.Id.ToString(), pair.Value.Name));
            }
        }

        internal void PlayerJoin(string id, string name)
        {
            ulong userId = ulong.Parse(id);

            if (playerData.TryGetValue(id, out PlayerRecord record))
            {
                record.Name = name;
                playerData[id] = record;
                allPlayers.Remove(id);
                allPlayers.Add(id, new SevenDaysPlayer(id, name));
            }
            else
            {
                record = new PlayerRecord { Id = userId, Name = name };
                playerData.Add(id, record);
                allPlayers.Add(id, new SevenDaysPlayer(id, name));
            }
        }

        internal void PlayerConnected(ClientInfo clientInfo)
        {
            string playerId = ((UserIdentifierSteam)clientInfo.PlatformId).ReadablePlatformUserIdentifier;
            SevenDaysPlayer player = new SevenDaysPlayer(clientInfo);
            allPlayers[playerId] = player;
            connectedPlayers[playerId] = player;
        }

        internal void PlayerDisconnected(ClientInfo clientInfo) => connectedPlayers.Remove(((UserIdentifierSteam)clientInfo.PlatformId).ReadablePlatformUserIdentifier);

        internal void SavePlayerData() => ProtoStorage.Save(playerData, "oxide.covalence");

        #region Player Finding

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values;

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values;

        /// <summary>
        /// Gets all sleeping players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Sleeping => null; // TODO: Implement if/when possible

        /// <summary>
        /// Finds a single player given unique ID
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public IPlayer FindPlayerById(string playerId)
        {
            return allPlayers.TryGetValue(playerId, out SevenDaysPlayer player) ? player : null;
        }

        /// <summary>
        /// Finds a single connected player given game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IPlayer FindPlayerByObj(object obj) => connectedPlayers.Values.FirstOrDefault(p => p.Object == obj);

        /// <summary>
        /// Finds a single player given a partial name or unique ID (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialNameOrId)
        {
            IPlayer[] players = FindPlayers(partialNameOrId).ToArray();
            return players.Length == 1 ? players[0] : null;
        }

        /// <summary>
        /// Finds any number of players given a partial name or unique ID (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            List<IPlayer> foundPlayers = new List<IPlayer>();

            foreach (SevenDaysPlayer player in connectedPlayers.Values)
            {
                if (player.Name.Equals(partialNameOrId, StringComparison.OrdinalIgnoreCase) || player.Id == partialNameOrId)
                {
                    foundPlayers = new List<IPlayer> { player };
                    break;
                }

                if (player.Name.IndexOf(partialNameOrId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundPlayers.Add(player);
                }
            }

            if (foundPlayers.Count > 0)
            {
                return foundPlayers;
            }

            foreach (SevenDaysPlayer player in allPlayers.Values)
            {
                if (player.Name.Equals(partialNameOrId, StringComparison.OrdinalIgnoreCase) || player.Id == partialNameOrId)
                {
                    foundPlayers = new List<IPlayer> { player };
                    break;
                }

                if (player.Name.IndexOf(partialNameOrId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundPlayers.Add(player);
                }
            }

            return foundPlayers;
        }

        #endregion Player Finding
    }
}
