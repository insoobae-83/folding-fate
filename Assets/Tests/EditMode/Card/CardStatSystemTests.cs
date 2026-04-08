using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class CardStatSystemTests
    {
        private CardStatSystem _system;
        private BaseCard _baseCard;

        [SetUp]
        public void SetUp()
        {
            _system = new CardStatSystem();
            _baseCard = new BaseCard(
                id: "standard_spade_ace",
                category: CardCategory.Standard,
                suit: Suit.Spade,
                rank: Rank.Ace,
                displayName: "Ace of Spades",
                description: "");
        }

        [Test]
        public void GetStatValue_SingleModifier_ReturnsValue()
        {
            var variant = new CardVariant(
                id: "attack_spade_ace",
                baseCard: _baseCard,
                displayName: "Attack Ace",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier> { new StatModifier(StatType.Attack, 3f) });

            Assert.AreEqual(3f, _system.GetStatValue(variant, StatType.Attack));
        }

        [Test]
        public void GetStatValue_DuplicateStatType_ReturnsSummed()
        {
            var variant = new CardVariant(
                id: "double_attack_spade_ace",
                baseCard: _baseCard,
                displayName: "Double Attack Ace",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>
                {
                    new StatModifier(StatType.Attack, 3f),
                    new StatModifier(StatType.Attack, 5f)
                });

            Assert.AreEqual(8f, _system.GetStatValue(variant, StatType.Attack));
        }

        [Test]
        public void GetStatValue_MissingStatType_ReturnsZero()
        {
            var variant = new CardVariant(
                id: "plain_spade_ace",
                baseCard: _baseCard,
                displayName: "Ace of Spades",
                skinId: "",
                element: Element.None,
                statModifiers: new List<StatModifier>());

            Assert.AreEqual(0f, _system.GetStatValue(variant, StatType.Attack));
        }
    }
}
