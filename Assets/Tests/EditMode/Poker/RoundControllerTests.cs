using System.Collections.Generic;
using NUnit.Framework;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Tests.EditMode.Poker
{
    [TestFixture]
    public class RoundControllerTests
    {
        private DeckModel _deck;
        private HandModel _hand;
        private DealSystem _dealSystem;
        private PokerViewModel _vm;
        private RoundController _controller;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _hand = new HandModel(maxHandSize: 8);
            _dealSystem = new DealSystem(_deck, _hand, new HandEvaluator());
            _vm = new PokerViewModel(_hand, _deck);
            _controller = new RoundController(_dealSystem, _vm);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            _vm.Dispose();
            _hand.Dispose();
            _deck.Dispose();
        }

        [Test]
        public void Start_DealsMaxHandSizeCards()
        {
            _controller.Start();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }

        [Test]
        public void Start_DeckHas44CardsRemaining()
        {
            _controller.Start();
            Assert.AreEqual(44, _deck.RemainingCount.Value);
        }

        [Test]
        public void SubmitCommand_UpdatesHandResultText()
        {
            _controller.Start();
            // Select 1 card and submit to get a result (at minimum HighCard)
            _vm.ToggleSelectCommand.Execute(0);
            _vm.SubmitCommand.Execute();
            Assert.IsFalse(string.IsNullOrEmpty(_vm.HandResultText.Value),
                "HandResultText should be set after submit");
        }

        [Test]
        public void ToggleSelectCommand_SelectsCard()
        {
            _controller.Start();
            _vm.ToggleSelectCommand.Execute(0);
            Assert.AreEqual(1, _hand.SelectedCount);
        }

        [Test]
        public void DrawCommand_FillsHandToMaxHandSize()
        {
            _controller.Start();
            // Manually reduce hand
            _hand.Clear();
            var partial = new List<BaseCard>
            {
                new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
                new("s2", CardCategory.Standard, Suit.Spade, Rank.Two, "", ""),
                new("s3", CardCategory.Standard, Suit.Spade, Rank.Three, "", ""),
            };
            _hand.AddCards(partial);
            Assert.AreEqual(3, _hand.Cards.Value.Count);

            _vm.DrawCommand.Execute();
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }
    }
}
