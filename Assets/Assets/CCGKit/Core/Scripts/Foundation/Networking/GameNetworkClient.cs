// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;
using System.Collections.Generic;

namespace CCGKit
{
    /// <summary>
    /// This class is responsible for handling the reception of custom network messages from the
    /// game server and routing them to the appropriate local player. Single-player mode is
    /// implemented via a second local player that uses the same system as normal human players
    /// in multiplayer modes; which is specially convenient implementation-wise (as there is no
    /// special case for it).
    /// </summary>
    public class GameNetworkClient : NetworkBehaviour
    {
        /// <summary>
        /// List of all the local players connected to this client. Normally this will only contain
        /// the human local player for multiplayer games, but in the case of single-player games it
        /// will also contain the AI-controlled player.
        /// </summary>
        protected List<Player> localPlayers = new List<Player>();

        /// <summary>
        /// Unity's OnStartClient.
        /// </summary>
        public override void OnStartClient()
        {
            RegisterNetworkHandlers();
        }

        /// <summary>
        /// Unity's OnDestroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnregisterNetworkHandlers();
        }

        /// <summary>
        /// Addds a new local player to this client.
        /// </summary>
        /// <param name="player">The local player to add to this client.</param>
        public void AddLocalPlayer(Player player)
        {
            localPlayers.Add(player);
        }

        /// <summary>
        /// Registers the network handlers for the network messages we are interested in handling.
        /// </summary>
        protected virtual void RegisterNetworkHandlers()
        {
            NetworkClient.RegisterHandler<StartGameMessage>(OnStartGame);
            NetworkClient.RegisterHandler<EndGameMessage>(OnEndGame);
            NetworkClient.RegisterHandler<StartTurnMessage>(OnStartTurn);
            NetworkClient.RegisterHandler<EndTurnMessage>(OnEndTurn);

            NetworkClient.RegisterHandler<CardMovedMessage>(OnCardMoved);
            NetworkClient.RegisterHandler<PlayerAttackedMessage>(OnPlayerAttacked);
            NetworkClient.RegisterHandler<CreatureAttackedMessage>(OnCreatureAttacked);

            NetworkClient.RegisterHandler<ActivateAbilityMessage>(OnActivateAbility);

            NetworkClient.RegisterHandler<PlayerDrewCardsMessage>(OnPlayerDrewCards);
            NetworkClient.RegisterHandler<OpponentDrewCardsMessage>(OnOpponentDrewCards);
        }

        /// <summary>
        /// Unregisters the network handlers for the network messages we are interested in handling.
        /// </summary>
        protected virtual void UnregisterNetworkHandlers()
        {
            NetworkClient.UnregisterHandler<OpponentDrewCardsMessage>();
            NetworkClient.UnregisterHandler<PlayerDrewCardsMessage>();

            NetworkClient.UnregisterHandler<ActivateAbilityMessage>();

            NetworkClient.UnregisterHandler<CreatureAttackedMessage>();
            NetworkClient.UnregisterHandler<PlayerAttackedMessage>();
            NetworkClient.UnregisterHandler<CardMovedMessage>();

            NetworkClient.UnregisterHandler<EndTurnMessage>();
            NetworkClient.UnregisterHandler<StartTurnMessage>();
            NetworkClient.UnregisterHandler<EndGameMessage>();
            NetworkClient.UnregisterHandler<StartGameMessage>();
        }

        /// <summary>
        /// Called when the game starts.
        /// </summary>
        /// <param name="netMsg">Start game message.</param>
        protected void OnStartGame(StartGameMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity == msg.recipientNetId);
            if (player != null)
            {
                player.OnStartGame(msg);
            }
        }

        /// <summary>
        /// Called when the game ends.
        /// </summary>
        /// <param name="netMsg">End game message.</param>
        protected void OnEndGame(EndGameMessage msg)
        {
            foreach (var player in localPlayers)
                player.OnEndGame(msg);
        }

        /// <summary>
        /// Called when a new turn starts.
        /// </summary>
        /// <param name="netMsg">Start turn message.</param>
        protected void OnStartTurn(StartTurnMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity == msg.recipientNetId);
            if (player != null)
                player.OnStartTurn(msg);
        }

        /// <summary>
        /// Called when a new turn ends.
        /// </summary>
        /// <param name="netMsg">End turn message.</param>
        protected void OnEndTurn(EndTurnMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity == msg.recipientNetId);
            if (player != null)
                player.OnEndTurn(msg);
        }

        /// <summary>
        /// Called when a card was moved from one zone to another.
        /// </summary>
        /// <param name="netMsg">Network message.</param>
        protected void OnCardMoved(CardMovedMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity != msg.playerNetId);
            if (player != null)
            {
                player.OnCardMoved(msg);
            }
        }

        /// <summary>
        /// Called when a player was attacked.
        /// </summary>
        /// <param name="netMsg">Network message.</param>
        protected void OnPlayerAttacked(PlayerAttackedMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity != msg.attackingPlayerNetId);
            if (player != null)
            {
                player.OnPlayerAttacked(msg);
            }
        }

        /// <summary>
        /// Called when a creature was attacked.
        /// </summary>
        /// <param name="netMsg">Network message.</param>
        protected void OnCreatureAttacked(CreatureAttackedMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity != msg.attackingPlayerNetId);
            if (player != null)
            {
                player.OnCreatureAttacked(msg);
            }
        }

        /// <summary>
        /// Called when an activated ability was activated.
        /// </summary>
        /// <param name="netMsg">Network message.</param>
        protected void OnActivateAbility(ActivateAbilityMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity != msg.playerNetId);
            if (player != null)
            {
                player.OnActivateAbility(msg);
            }
        }

        /// <summary>
        /// Called when a player draws cards.
        /// </summary>
        /// <param name="netMsg">Network message.</param>
        protected void OnPlayerDrewCards(PlayerDrewCardsMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity == msg.playerNetId);
            if (player != null)
            {
                player.OnPlayerDrewCards(msg);
            }
        }

        /// <summary>
        /// Called when an opponent draws cards.
        /// </summary>
        /// <param name="netMsg">Network message.</param>
        protected void OnOpponentDrewCards(OpponentDrewCardsMessage msg)
        {
            var player = localPlayers.Find(x => x.netIdentity == msg.playerNetId);
            if (player != null)
            {
                player.OnOpponentDrewCards(msg);
            }
        }
    }
}
