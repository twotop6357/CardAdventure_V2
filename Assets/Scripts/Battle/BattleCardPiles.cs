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
            if (!RemoveHandCard(card))
            {
                return false;
            }

            AddToDiscard(card);
            return true;
        }

        public bool MoveHandCardToExhaust(BattleRuntimeCard card)
        {
            if (!RemoveHandCard(card))
            {
                return false;
            }

            AddToExhaust(card);
            return true;
        }

        public bool MoveHandCardToDrawPile(BattleRuntimeCard card)
        {
            if (!RemoveHandCard(card))
            {
                return false;
            }

            AddToDrawPile(card);
            return true;
        }

        public bool RemoveHandCard(BattleRuntimeCard card)
        {
            return card != null && hand.Remove(card);
        }

        public void AddToDiscard(BattleRuntimeCard card)
        {
            if (card == null)
            {
                return;
            }

            discardPile.Add(card);
        }

        public void AddToExhaust(BattleRuntimeCard card)
        {
            if (card == null)
            {
                return;
            }

            exhaustPile.Add(card);
        }

        public void AddToDrawPile(BattleRuntimeCard card)
        {
            if (card == null)
            {
                return;
            }

            int insertIndex = Random.Range(0, drawPile.Count + 1);
            drawPile.Insert(insertIndex, card);
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

        public void ClearTemporaryEnergyCosts()
        {
            ClearTemporaryEnergyCosts(drawPile);
            ClearTemporaryEnergyCosts(hand);
            ClearTemporaryEnergyCosts(discardPile);
            ClearTemporaryEnergyCosts(exhaustPile);
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

        private static void ClearTemporaryEnergyCosts(List<BattleRuntimeCard> cards)
        {
            foreach (BattleRuntimeCard card in cards)
            {
                card?.ClearTemporaryEnergyCost();
            }
        }
    }
}
