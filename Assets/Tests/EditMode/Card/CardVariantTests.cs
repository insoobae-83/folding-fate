using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class CardVariantTests
    {
        private BaseCard _baseSpadeAce;

        [SetUp]
        public void SetUp()
        {
            _baseSpadeAce = new BaseCard(
                id: "standard_spade_ace",
                category: CardCategory.Standard,
                suit: Suit.Spade,
                rank: Rank.Ace,
                displayName: "Ace of Spades",
                description: "");
        }

        [Test]
        public void Constructor_FullVariant_SetsAllProperties()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 3f)
            };

            var variant = new CardVariant(
                id: "fire_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Fire Ace of Spades",
                skinId: "skin_fire",
                element: Element.Fire,
                statModifiers: modifiers);

            Assert.AreEqual("fire_spade_ace", variant.Id);
            Assert.AreSame(_baseSpadeAce, variant.BaseCard);
            Assert.AreEqual("Fire Ace of Spades", variant.DisplayName);
            Assert.AreEqual("skin_fire", variant.SkinId);
            Assert.AreEqual(Element.Fire, variant.Element);
            Assert.AreEqual(1, variant.StatModifiers.Count);
        }

        [Test]
        public void HasElement_NoElement_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasElement);
        }

        [Test]
        public void HasElement_WithElement_ReturnsTrue()
        {
            var variant = new CardVariant(
                id: "water_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Water Ace of Spades",
                skinId: "",
                element: Element.Water,
                statModifiers: new List<StatModifier>());

            Assert.IsTrue(variant.HasElement);
        }

        [Test]
        public void HasSkin_EmptySkinId_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasSkin);
        }

        [Test]
        public void HasSkin_NullSkinId_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: null,
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasSkin);
        }

        [Test]
        public void HasSkin_WithSkinId_ReturnsTrue()
        {
            var variant = new CardVariant(
                id: "golden_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Golden Ace of Spades",
                skinId: "skin_golden",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsTrue(variant.HasSkin);
        }

        [Test]
        public void HasStatModifiers_Empty_ReturnsFalse()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.IsFalse(variant.HasStatModifiers);
        }

        [Test]
        public void HasStatModifiers_WithModifiers_ReturnsTrue()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 5f)
            };

            var variant = new CardVariant(
                id: "strong_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Strong Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: modifiers);

            Assert.IsTrue(variant.HasStatModifiers);
        }

        [Test]
        public void StatModifiers_IsReadOnly_CannotModifyExternally()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Attack, 3f)
            };

            var variant = new CardVariant(
                id: "attack_spade_ace",
                baseCard: _baseSpadeAce,
                displayName: "Attack Ace",
                skinId: "",
                element: Element.None,
                statModifiers: modifiers);

            modifiers.Add(new StatModifier(StatType.Point, 10f));
            Assert.AreEqual(1, variant.StatModifiers.Count);
        }
    }
}
