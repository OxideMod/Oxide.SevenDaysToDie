extern alias References;

using References::ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using uMod.Libraries.Universal;

namespace uMod.SevenDaysToDie
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class SevenDaysToDiePlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, SevenDaysToDiePlayer> allPlayers;
        private IDictionary<string, SevenDaysToDiePlayer> connectedPlayers;
        private const string dataFileName = "umod";

        internal void Initialize()
        {
            // TODO: Migrate/move from oxide.covalence.data to umod.data if SQLite is not used, else migrate to umod.db with SQLite
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>(dataFileName) ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, SevenDaysToDiePlayer>();
            connectedPlayers = new Dictionary<string, SevenDaysToDiePlayer>();

            foreach (KeyValuePair<string, PlayerRecord> pair in playerData)
            {
                allPlayers.Add(pair.Key, new SevenDaysToDiePlayer(pair.Value.Id.ToString(), pair.Value.Name));
            }
        }

        internal void PlayerJoin(string id, string name)
        {
            ulong userId = ulong.Parse(id);

            PlayerRecord record;
            if (playerData.TryGetValue(id, out record))
            {
                record.Name = name;
                playerData[id] = record;
                allPlayers.Remove(id);
                allPlayers.Add(id, new SevenDaysToDiePlayer(id, name));
            }
            else
            {
                record = new PlayerRecord { Id = userId, Name = name };
                playerData.Add(id, record);
                allPlayers.Add(id, new SevenDaysToDiePlayer(id, name));
            }
        }

        internal void PlayerConnected(ClientInfo client)
        {
            SevenDaysToDiePlayer player = new SevenDaysToDiePlayer(client);
            allPlayers[client.playerId] = player;
            connectedPlayers[client.playerId] = player;
        }

        internal void PlayerDisconnected(ClientInfo client) => connectedPlayers.Remove(client.playerId);

        internal void SavePlayerData() => ProtoStorage.Save(playerData, dataFileName);

        #region Player Finding

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all sleeping players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Sleeping => null; // TODO: Implement if/when possible

        /// <summary>
        /// Finds a single player given unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer FindPlayerById(string id)
        {
            SevenDaysToDiePlayer player;
            return allPlayers.TryGetValue(id, out player) ? player : null;
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
            foreach (SevenDaysToDiePlayer player in allPlayers.Values)
            {
                if (player.Name != null && player.Name.IndexOf(partialNameOrId, StringComparison.OrdinalIgnoreCase) >= 0 || player.Id == partialNameOrId)
                {
                    yield return player;
                }
            }
        }

        #endregion Player Finding
    }
}
