using System;

namespace CardAdventure
{
    /// <summary>
    /// Runtime instance of a card in battle. Multiple copies can point to the same CardData.
    /// </summary>
    [Serializable]
    public sealed class BattleRuntimeCard
    {
        public BattleRuntimeCard(CardData data)
        {
            Data = data;
            InstanceId = Guid.NewGuid().ToString("N");
        }

        public string InstanceId { get; }

        public CardData Data { get; }

        public string DisplayName => Data != null ? Data.cardName : string.Empty;

        public int EnergyCost => Data != null ? Data.energyCost : 0;

        public bool IsExhaust => Data != null && Data.isExhaust;
    }
}
