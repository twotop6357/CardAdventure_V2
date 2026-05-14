using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CardAdventure.Tests
{
    public sealed class ShopUIControllerEditModeTests
    {
        private readonly List<Object> spawnedObjects = new List<Object>();
        private GameDataManager gameData;
        private int originalGold;
        private List<CardData> originalDeck;
        private JobClassInfo originalJobInfo;
        private bool usesSceneGameData;

        [TearDown]
        public void TearDown()
        {
            if (usesSceneGameData && gameData != null)
            {
                gameData.Gold = originalGold;
                gameData.SelectedJobInfo = originalJobInfo;
                gameData.Deck.Clear();
                if (originalDeck != null)
                {
                    gameData.Deck.AddRange(originalDeck);
                }
            }

            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(spawnedObjects[i]);
                }
            }

            if (!usesSceneGameData)
            {
                SetGameDataInstance(null);
            }

            spawnedObjects.Clear();
        }

        [Test]
        public void TryBuyOffer_SpendsGoldAndAddsCardToDeck()
        {
            GameDataManager gameData = CreateOrFindGameDataManager();
            gameData.Gold = 100;

            CardData card = CreateCard("테스트 카드", CardGrade.Common);
            ShopUIController shop = CreateShop(card);

            shop.Open();

            Assert.AreEqual(1, shop.CurrentOfferCount);
            Assert.IsTrue(shop.TryBuyOffer(0));
            Assert.AreEqual(55, gameData.Gold);
            Assert.Contains(card, gameData.Deck);
            Assert.IsFalse(shop.TryBuyOffer(0));
        }

        [Test]
        public void Open_OnlyOffersCardsMatchingSelectedJobClass()
        {
            GameDataManager gameData = CreateOrFindGameDataManager();
            gameData.Gold = 100;
            gameData.SelectedJobInfo = CreateJob(CardClass.Mage);

            CardData warriorCard = CreateCard("전사 카드", CardGrade.Common, CardClass.Warrior);
            CardData mageCard = CreateCard("마법사 카드", CardGrade.Common, CardClass.Mage);
            ShopUIController shop = CreateShop(warriorCard, mageCard);

            shop.Open();

            Assert.AreEqual(1, shop.CurrentOfferCount);
            Assert.IsTrue(shop.TryBuyOffer(0));
            Assert.Contains(mageCard, gameData.Deck);
            Assert.IsFalse(gameData.Deck.Contains(warriorCard));
        }

        [Test]
        public void OpenAfterClose_RecreatesSingleCardPreviewPerOfferSlot()
        {
            GameDataManager gameData = CreateOrFindGameDataManager();
            gameData.Gold = 100;

            CardData first = CreateCard("첫 번째 카드", CardGrade.Common);
            CardData second = CreateCard("두 번째 카드", CardGrade.Common);
            CardData third = CreateCard("세 번째 카드", CardGrade.Common);
            ShopUIController shop = CreateShop(first, second, third);

            shop.Open();
            AssertPreviewParentChildCounts(shop, 1);

            shop.Close();
            shop.Open();
            AssertPreviewParentChildCounts(shop, 1);
        }

        private GameDataManager CreateOrFindGameDataManager()
        {
            gameData = GameDataManager.Instance != null
                ? GameDataManager.Instance
                : Object.FindFirstObjectByType<GameDataManager>();

            if (gameData != null)
            {
                SetGameDataInstance(gameData);
                usesSceneGameData = true;
                originalGold = gameData.Gold;
                originalDeck = new List<CardData>(gameData.Deck);
                originalJobInfo = gameData.SelectedJobInfo;
                return gameData;
            }

            usesSceneGameData = false;
            GameObject go = new GameObject("GameDataManager_Test");
            spawnedObjects.Add(go);
            gameData = go.AddComponent<GameDataManager>();
            SetGameDataInstance(gameData);
            originalGold = gameData.Gold;
            originalDeck = new List<CardData>(gameData.Deck);
            originalJobInfo = gameData.SelectedJobInfo;
            return gameData;
        }

        private void SetGameDataInstance(GameDataManager manager)
        {
            PropertyInfo property = typeof(GameDataManager).GetProperty(
                "Instance",
                BindingFlags.Static | BindingFlags.Public);
            property?.GetSetMethod(true)?.Invoke(null, new object[] { manager });
        }

        private ShopUIController CreateShop(params CardData[] cards)
        {
            GameObject go = new GameObject("ShopUI_Test");
            spawnedObjects.Add(go);

            ShopUIController shop = go.AddComponent<ShopUIController>();
            shop.SetCardPool(new List<CardData>(cards));
            return shop;
        }

        private CardData CreateCard(string cardName, CardGrade grade, CardClass cardClass = CardClass.Warrior)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            spawnedObjects.Add(card);
            card.cardName = cardName;
            card.grade = grade;
            card.cardType = CardType.Skill;
            card.cardClass = cardClass;
            card.energyCost = 1;
            card.effectDescription = "테스트용 카드입니다.";
            return card;
        }

        private JobClassInfo CreateJob(CardClass cardClass)
        {
            JobClassInfo job = ScriptableObject.CreateInstance<JobClassInfo>();
            spawnedObjects.Add(job);
            job.cardClass = cardClass;
            job.displayName = cardClass.ToString();
            return job;
        }

        private void AssertPreviewParentChildCounts(ShopUIController shop, int expectedCount)
        {
            FieldInfo field = typeof(ShopUIController).GetField(
                "offerViews",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            System.Collections.IList offerViews = field.GetValue(shop) as System.Collections.IList;
            Assert.NotNull(offerViews);
            Assert.GreaterOrEqual(offerViews.Count, shop.CurrentOfferCount);

            for (int i = 0; i < shop.CurrentOfferCount; i++)
            {
                object offerView = offerViews[i];
                PropertyInfo previewParentProperty = offerView.GetType().GetProperty("PreviewParent");
                PropertyInfo cardPreviewProperty = offerView.GetType().GetProperty("CardPreview");
                Assert.NotNull(previewParentProperty);
                Assert.NotNull(cardPreviewProperty);

                Transform previewParent = previewParentProperty.GetValue(offerView) as Transform;
                BattleCardView cardPreview = cardPreviewProperty.GetValue(offerView) as BattleCardView;
                Assert.NotNull(previewParent);
                Assert.NotNull(cardPreview);
                Assert.AreSame(previewParent, cardPreview.transform.parent);
                Assert.AreEqual(expectedCount, previewParent.childCount);
            }
        }
    }
}
