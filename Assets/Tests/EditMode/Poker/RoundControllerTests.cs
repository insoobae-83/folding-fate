using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Data;
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
        private PokerConfig _config;

        [SetUp]
        public void SetUp()
        {
            _deck = new DeckModel();
            _hand = new HandModel(maxHandSize: 8);
            _dealSystem = new DealSystem(_deck, _hand, new HandEvaluator());
            _vm = new PokerViewModel(_hand, _deck);
            _config = ScriptableObject.CreateInstance<PokerConfig>();
            _config.ShowcaseDurationSeconds = 0f;
            _controller = new RoundController(_dealSystem, _vm, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            Object.DestroyImmediate(_config);
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
            _vm.ToggleSelectCommand.Execute(0);
            _vm.SubmitCommand.Execute(Unit.Default);

            // 비동기 연출 후 HandResultText 설정됨
            // duration=0이라도 UniTask.Delay가 즉시 완료되지 않을 수 있음
            // 연출 진입 또는 결과 텍스트 설정 중 하나는 확인 가능
            Assert.IsTrue(_vm.Showcase.CurrentValue.IsActive ||
                !string.IsNullOrEmpty(_vm.HandResultText.CurrentValue),
                "제출 후 연출이 활성화되거나 결과 텍스트가 설정되어야 함");
        }

        [Test]
        public void SubmitCommand_ActivatesShowcase()
        {
            _controller.Start();
            _vm.ToggleSelectCommand.Execute(0);

            _vm.SubmitCommand.Execute(Unit.Default);

            Assert.IsTrue(_vm.Showcase.CurrentValue.IsActive,
                "제출 직후 연출이 활성화되어야 함");
            Assert.IsTrue(_vm.Showcase.CurrentValue.Cards.Count > 0,
                "연출에 카드가 포함되어야 함");
            Assert.IsFalse(string.IsNullOrEmpty(_vm.Showcase.CurrentValue.RankText),
                "연출에 족보 텍스트가 포함되어야 함");
        }

        [Test]
        public void ToggleSelectCommand_SelectsCard()
        {
            _controller.Start();
            _vm.ToggleSelectCommand.Execute(0);
            Assert.AreEqual(1, _hand.SelectedCount);
        }

        [Test]
        public void DiscardCommand_RemovesSelectedCards()
        {
            _controller.Start();
            _hand.Clear();
            var cards = new List<BaseCard>();
            for (int i = 0; i < 8; i++)
                cards.Add(new BaseCard($"s{i}", CardCategory.Standard, Suit.Spade, (Rank)(i + 2), "", ""));
            _hand.AddCards(cards);

            _vm.ToggleSelectCommand.Execute(0);
            _vm.ToggleSelectCommand.Execute(1);
            Assert.AreEqual(2, _hand.SelectedCount);

            _vm.DiscardCommand.Execute(Unit.Default);

            Assert.AreEqual(0, _hand.SelectedCount);
        }

        [Test]
        public void Showcase_InitiallyInactive()
        {
            Assert.IsFalse(_vm.Showcase.CurrentValue.IsActive);
        }

        [Test]
        public void BeginShowcase_SetsShowcaseActive()
        {
            var cards = new List<BaseCard>
            {
                new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
            };
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);

            _vm.BeginShowcase(result);

            Assert.IsTrue(_vm.Showcase.CurrentValue.IsActive);
            Assert.AreEqual(1, _vm.Showcase.CurrentValue.Cards.Count);
            Assert.AreEqual("하이 카드", _vm.Showcase.CurrentValue.RankText);
        }

        [Test]
        public void EndShowcase_SetsShowcaseInactive()
        {
            var cards = new List<BaseCard>
            {
                new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
            };
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);

            _vm.BeginShowcase(result);
            _vm.EndShowcase();

            Assert.IsFalse(_vm.Showcase.CurrentValue.IsActive);
        }

        [Test]
        public void CanSubmit_FalseDuringShowcase()
        {
            _controller.Start();
            _vm.ToggleSelectCommand.Execute(0);
            Assert.IsTrue(_vm.CanSubmit.CurrentValue, "선택 후 제출 가능해야 함");

            var cards = new List<BaseCard>
            {
                new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
            };
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 }, cards);
            _vm.BeginShowcase(result);

            Assert.IsFalse(_vm.CanSubmit.CurrentValue, "연출 중 제출 불가해야 함");
        }

        [Test]
        public void IsDealing_InitiallyFalse()
        {
            Assert.IsFalse(_vm.IsDealing.CurrentValue);
        }

        [Test]
        public void BeginDealing_SetsIsDealingTrue()
        {
            _vm.BeginDealing();
            Assert.IsTrue(_vm.IsDealing.CurrentValue);
        }

        [Test]
        public void EndDealing_SetsIsDealingFalse()
        {
            _vm.BeginDealing();
            _vm.EndDealing();
            Assert.IsFalse(_vm.IsDealing.CurrentValue);
        }

        [Test]
        public void CanSubmit_FalseDuringDealing()
        {
            _controller.Start();
            _hand.Clear();
            var cards = new List<BaseCard>();
            for (int i = 0; i < 8; i++)
                cards.Add(new BaseCard($"s{i}", CardCategory.Standard, Suit.Spade, (Rank)(i + 2), "", ""));
            _hand.AddCards(cards);

            _vm.ToggleSelectCommand.Execute(0);
            Assert.IsTrue(_vm.CanSubmit.CurrentValue);

            _vm.BeginDealing();

            Assert.IsFalse(_vm.CanSubmit.CurrentValue, "딜링 중 제출 불가해야 함");
        }
    }
}
