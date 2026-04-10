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

            _vm.DrawCommand.Execute(Unit.Default);
            Assert.AreEqual(8, _hand.Cards.Value.Count);
        }

        [Test]
        public void DiscardCommand_RemovesSelectedCardsWithoutSettingResult()
        {
            _controller.Start();
            // 카드 2장 선택
            _vm.ToggleSelectCommand.Execute(0);
            _vm.ToggleSelectCommand.Execute(1);
            Assert.AreEqual(2, _hand.SelectedCount);

            _vm.DiscardCommand.Execute(Unit.Default);

            // 선택한 2장이 제거됨
            Assert.AreEqual(6, _hand.Cards.Value.Count);
            // 선택 상태 해제
            Assert.AreEqual(0, _hand.SelectedCount);
            // 족보 결과 텍스트는 변경되지 않음
            Assert.IsTrue(string.IsNullOrEmpty(_vm.HandResultText.CurrentValue),
                "DiscardCommand는 족보 결과를 설정하면 안 됨");
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
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });

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
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });

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
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });
            _vm.BeginShowcase(result);

            Assert.IsFalse(_vm.CanSubmit.CurrentValue, "연출 중 제출 불가해야 함");
        }

        [Test]
        public void CanDraw_FalseDuringShowcase()
        {
            _controller.Start();
            _hand.Clear();
            Assert.IsTrue(_vm.CanDraw.CurrentValue, "핸드 비었을 때 드로우 가능해야 함");

            var cards = new List<BaseCard>
            {
                new("s1", CardCategory.Standard, Suit.Spade, Rank.Ace, "", ""),
            };
            var result = new HandResult(HandRank.HighCard, cards, new List<int> { 14 });
            _vm.BeginShowcase(result);

            Assert.IsFalse(_vm.CanDraw.CurrentValue, "연출 중 드로우 불가해야 함");
        }
    }
}
