// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using Mirror;

namespace CCGKit
{
    /// <summary>
    /// This server handler is responsible for managing client requests for playing new cards.
    /// </summary>
    public class PlayCardHandler : ServerHandler
    {
        public PlayCardHandler(Server server) : base(server)
        {
        }

        public override void RegisterNetworkHandlers()
        {
            base.RegisterNetworkHandlers();
            NetworkServer.RegisterHandler<MoveCardMessage>(OnMoveCard);
        }

        public override void UnregisterNetworkHandlers()
        {
            NetworkServer.UnregisterHandler<MoveCardMessage>();
            base.UnregisterNetworkHandlers();
        }

        protected virtual void OnMoveCard(NetworkConnection conn, MoveCardMessage msg)
        {
            // Only the current player can summon cards.
            if (conn.identity.netId != server.gameState.currentPlayer.connectionId)
            {
                return;
            }

            var player = server.gameState.players.Find(x => x.netId == msg.playerNetId);
            var originZone = player.zones[msg.originZoneId];
            var destinationZone = player.zones[msg.destinationZoneId];

            var card = originZone.cards.Find(x => x.instanceId == msg.cardInstanceId);
            if (card != null)
            {
                var gameConfig = GameManager.Instance.config;
                var libraryCard = gameConfig.GetCard(card.cardId);
                var cost = libraryCard.costs.Find(x => x is PayResourceCost);
                if (cost != null)
                {
                    var payResourceCost = cost as PayResourceCost;
                    player.stats[payResourceCost.statId].baseValue -= payResourceCost.value;
                }

                var cardMovedMsg = new CardMovedMessage();
                cardMovedMsg.playerNetId = msg.playerNetId;
                cardMovedMsg.card = NetworkingUtils.GetNetCard(card);
                cardMovedMsg.originZoneId = originZone.zoneId;
                cardMovedMsg.destinationZoneId = destinationZone.zoneId;
                cardMovedMsg.targetInfo = msg.targetInfo;
                server.SafeSendToClient(server.gameState.currentOpponent, cardMovedMsg);

                server.effectSolver.MoveCard(player.netId, card, originZone.name, destinationZone.name, msg.targetInfo);
            }
        }
    }
}