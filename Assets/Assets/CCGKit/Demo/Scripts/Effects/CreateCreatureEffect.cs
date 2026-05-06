// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

namespace CCGKit
{
    [PlayerTarget]
    public class CreateCreatureEffect : PlayerEffect
    {
        [StringField("Card name", width = 100)]
        [Order(1)]
        public string cardName;

        [IntField("Amount")]
        [Order(2)]
        public int numCopies;

        public override void Resolve(GameState state, PlayerInfo player)
        {
            var libraryCard = state.config.cards.Find(x => x.name == cardName);
            if (libraryCard != null)
            {
                for (var i = 0; i < numCopies; i++)
                {
                    var card = new RuntimeCard
                    {
                        cardId = libraryCard.id,
                        instanceId = player.currentCardInstanceId++,
                        ownerPlayer = player
                    };

                    // Copy stats.
                    foreach (var stat in libraryCard.stats)
                    {
                        var statCopy = new Stat
                        {
                            statId = stat.statId,
                            name = stat.name,
                            originalValue = stat.originalValue,
                            baseValue = stat.baseValue,
                            minValue = stat.minValue,
                            maxValue = stat.maxValue
                        };
                        card.stats[stat.statId] = statCopy;
                        card.namedStats[stat.name] = statCopy;
                    }

                    // Copy keywords.
                    foreach (var keyword in libraryCard.keywords)
                    {
                        var keywordCopy = new RuntimeKeyword
                        {
                            keywordId = keyword.keywordId,
                            valueId = keyword.valueId
                        };
                        card.keywords.Add(keywordCopy);
                    }

                    player.namedZones["Board"].AddCardCreatedByEffect(card);
                }
            }
        }
    }
}
