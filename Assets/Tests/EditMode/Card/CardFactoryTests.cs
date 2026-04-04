using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using FoldingFate.Core;
using FoldingFate.Features.Card.Data;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class CardFactoryTests
    {
        private CardFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CardFactory();
        }

        [Test]
        public void CreateBaseCard_StandardCard_MapsAllFields()
        {
            var data = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(data, "standard_spade_ace", CardCategory.Standard,
                Suit.Spade, Rank.Ace, "Ace of Spades", "A standard card");

            var card = _factory.CreateBaseCard(data);

            Assert.AreEqual("standard_spade_ace", card.Id);
            Assert.AreEqual(CardCategory.Standard, card.Category);
            Assert.AreEqual(Suit.Spade, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
            Assert.AreEqual("Ace of Spades", card.DisplayName);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateBaseCard_JokerCard_SuitAndRankAreNull()
        {
            var data = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(data, "joker_red", CardCategory.Joker,
                Suit.Spade, Rank.Ace, "Red Joker", "");

            var card = _factory.CreateBaseCard(data);

            Assert.AreEqual(CardCategory.Joker, card.Category);
            Assert.IsNull(card.Suit);
            Assert.IsNull(card.Rank);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateBaseCard_CustomCard_SuitAndRankAreNull()
        {
            var data = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(data, "custom_wild", CardCategory.Custom,
                Suit.Spade, Rank.Ace, "Wild Card", "");

            var card = _factory.CreateBaseCard(data);

            Assert.AreEqual(CardCategory.Custom, card.Category);
            Assert.IsNull(card.Suit);
            Assert.IsNull(card.Rank);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateCardVariant_MapsAllFields()
        {
            var baseData = ScriptableObject.CreateInstance<BaseCardData>();
            SetBaseCardData(baseData, "standard_spade_ace", CardCategory.Standard,
                Suit.Spade, Rank.Ace, "Ace of Spades", "");

            var variantData = ScriptableObject.CreateInstance<CardVariantData>();
            SetCardVariantData(variantData, "fire_spade_ace", baseData,
                "Fire Ace", "skin_fire", Element.Fire,
                new List<StatModifier> { new StatModifier(StatType.Attack, 3f) });

            var baseCard = _factory.CreateBaseCard(baseData);
            var variant = _factory.CreateCardVariant(variantData, baseCard);

            Assert.AreEqual("fire_spade_ace", variant.Id);
            Assert.AreSame(baseCard, variant.BaseCard);
            Assert.AreEqual("Fire Ace", variant.DisplayName);
            Assert.AreEqual("skin_fire", variant.SkinId);
            Assert.AreEqual(Element.Fire, variant.Element);
            Assert.AreEqual(1, variant.StatModifiers.Count);
            Assert.AreEqual(3f, variant.GetStatValue(StatType.Attack));

            Object.DestroyImmediate(baseData);
            Object.DestroyImmediate(variantData);
        }

        private static void SetBaseCardData(
            BaseCardData data, string id, CardCategory category,
            Suit suit, Rank rank, string displayName, string description)
        {
            var so = new SerializedObject(data);
            so.FindProperty("<Id>k__BackingField").stringValue = id;
            so.FindProperty("<Category>k__BackingField").enumValueIndex = (int)category;
            so.FindProperty("<Suit>k__BackingField").enumValueIndex = (int)suit;
            so.FindProperty("<Rank>k__BackingField").enumValueIndex = (int)rank;
            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<Description>k__BackingField").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCardVariantData(
            CardVariantData data, string id, BaseCardData baseCard,
            string displayName, string skinId, Element element,
            List<StatModifier> statModifiers)
        {
            var so = new SerializedObject(data);
            so.FindProperty("<Id>k__BackingField").stringValue = id;
            so.FindProperty("<BaseCard>k__BackingField").objectReferenceValue = baseCard;
            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<SkinId>k__BackingField").stringValue = skinId;
            so.FindProperty("<Element>k__BackingField").enumValueIndex = (int)element;

            var modsProp = so.FindProperty("<StatModifiers>k__BackingField");
            modsProp.ClearArray();
            for (int i = 0; i < statModifiers.Count; i++)
            {
                modsProp.InsertArrayElementAtIndex(i);
                var elem = modsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Type").enumValueIndex = (int)statModifiers[i].Type;
                elem.FindPropertyRelative("Value").floatValue = statModifiers[i].Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
