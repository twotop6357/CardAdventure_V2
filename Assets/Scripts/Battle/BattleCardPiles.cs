using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Owns draw, hand, discard, and exhaust piles for a single battle.
    /// </summary>
    public sealed class BattleCardPiles
    {
        private readonly List<BattleRuntimeCard> drawPile = new List<BattleRuntimeCard>();
        private readonly List<BattleRuntimeCard> hand = new List<BattleRuntimeCard>();
        private readonly List<BattleRuntimeCard> discardPile = new List<BattleRuntimeCard>();
        private readonly List<BattleRuntimeCard> exhaustPile = new List<BattleRuntimeCard>();

        public IReadOnlyList<BattleRuntimeCard> DrawPile => drawPile;
        public IReadOnlyList<BattleRuntimeCard> Hand => hand;
        public IReadOnlyList<BattleRuntimeCard> DiscardPile => discardPile;
        public IReadOnlyList<BattleRuntimeCard> ExhaustPile => exhaustPile;

        public bool HasHandCard(BattleRuntimeCard card)
        {
            return card != null && hand.Contains(card);
        }

        public void Initialize(IEnumerable<CardData> deck)
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            exhaustPile.Clear();

            if (deck == null)
            {
                return;
            }

            foreach (CardData cardData in deck)
            {
                if (cardData != null)
                {
                    drawPile.Add(new BattleRuntimeCard(cardData));
                }
            }

            ShuffleDrawPile();
        }

        public List<BattleRuntimeCard> Draw(int count)
        {
            List<BattleRuntimeCard> drawnCards = new List<BattleRuntimeCard>();
            int drawCount = Mathf.Max(0, count);

            for (int i = 0; i < drawCount; i++)
            {
                RefillDrawPileIfNeeded();

                if (drawPile.Count == 0)
                {
                    break;
                }

                int lastIndex = drawPile.Count - 1;
                BattleRuntimeCard card = drawPile[lastIndex];
                drawPile.RemoveAt(lastIndex);
                hand.Add(card);
                drawnCards.Add(card);
            }

            return drawnCards;
        }

        public bool MoveHandCardToDiscard(BattleRuntimeCard card)
        {
            if (card == null || !hand.Remove(card))
            {
                return false;
            }

            discardPile.Add(card);
            return true;
        }

        public bool MoveHandCardToExhaust(BattleRuntimeCard card)
        {
            if (card == null || !hand.Remove(card))
            {
                return false;
            }

            exhaustPile.Add(card);
            return true;
        }

        public void DiscardHand()
        {
            if (hand.Count == 0)
            {
                return;
            }

            discardPile.AddRange(hand);
            hand.Clear();
        }

        public void ShuffleDrawPile()
        {
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (drawPile[i], drawPile[swapIndex]) = (drawPile[swapIndex], drawPile[i]);
            }
        }

        private void RefillDrawPileIfNeeded()
        {
            if (drawPile.Count > 0 || discardPile.Count == 0)
            {
                return;
            }

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDrawPile();
        }
    }
}
