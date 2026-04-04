using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Tests.EditMode.Card
{
    [TestFixture]
    public class BaseCardTests
    {
        [Test]
        public void Constructor_StandardCard_SetsProperties()
        {
            var card = new BaseCard(
                id: "standard_spade_ace",
                category: CardCategory.Standard,
                suit: Suit.Spade,
                rank: Rank.Ace,
                displayName: "Ace of Spades",
                description: "A standard ace of spades");

            Assert.AreEqual("standard_spade_ace", card.Id);
            Assert.AreEqual(CardCategory.Standard, card.Category);
            Assert.AreEqual(Suit.Spade, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
            Assert.AreEqual("Ace of Spades", card.DisplayName);
            Assert.AreEqual("A standard ace of spades", card.Description);
        }

        [Test]
        public void IsStandard_StandardCard_ReturnsTrue()
        {
            var card = new BaseCard(
                id: "standard_heart_king",
                category: CardCategory.Standard,
                suit: Suit.Heart,
                rank: Rank.King,
                displayName: "King of Hearts",
                description: "");

            Assert.IsTrue(card.IsStandard);
            Assert.IsFalse(card.IsJoker);
            Assert.IsFalse(card.IsCustom);
        }

        [Test]
        public void IsJoker_JokerCard_ReturnsTrue()
        {
            var card = new BaseCard(
                id: "joker_red",
                category: CardCategory.Joker,
                suit: null,
                rank: null,
                displayName: "Red Joker",
                description: "");

            Assert.IsTrue(card.IsJoker);
            Assert.IsFalse(card.IsStandard);
            Assert.IsNull(card.Suit);
            Assert.IsNull(card.Rank);
        }

        [Test]
        public void IsCustom_CustomCard_ReturnsTrue()
        {
            var card = new BaseCard(
                id: "custom_wild",
                category: CardCategory.Custom,
                suit: null,
                rank: null,
                displayName: "Wild Card",
                description: "A special custom card");

            Assert.IsTrue(card.IsCustom);
            Assert.IsFalse(card.IsStandard);
            Assert.IsFalse(card.IsJoker);
        }
    }
}
