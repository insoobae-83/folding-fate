using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Features.Poker.UI.ViewModels
{
    public class PokerViewModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveProperty<string> _handResultText;

        public ReadOnlyReactiveProperty<IReadOnlyList<BaseCard>> Hand { get; }
        public ReadOnlyReactiveProperty<IReadOnlyList<int>> SelectedIndices { get; }
        public ReadOnlyReactiveProperty<int> DeckRemaining { get; }
        public ReadOnlyReactiveProperty<string> HandResultText { get; }

        public ReadOnlyReactiveProperty<bool> CanSubmit { get; }
        public ReadOnlyReactiveProperty<bool> CanDraw { get; }

        public ReactiveCommand<int> ToggleSelectCommand { get; }
        public ReactiveCommand SubmitCommand { get; }
        public ReactiveCommand DrawCommand { get; }

        public PokerViewModel(HandModel hand, DeckModel deck)
        {
            _handResultText = new ReactiveProperty<string>(string.Empty).AddTo(_disposables);
            Hand = hand.Cards.ToReadOnlyReactiveProperty().AddTo(_disposables);
            SelectedIndices = hand.SelectedIndices.ToReadOnlyReactiveProperty().AddTo(_disposables);
            DeckRemaining = deck.RemainingCount.ToReadOnlyReactiveProperty().AddTo(_disposables);
            HandResultText = _handResultText.ToReadOnlyReactiveProperty().AddTo(_disposables);

            ToggleSelectCommand = new ReactiveCommand<int>().AddTo(_disposables);

            var canSubmit = hand.SelectedIndices.Select(indices => indices.Count >= 1 && indices.Count <= 5);
            CanSubmit = canSubmit.ToReadOnlyReactiveProperty(initialValue: false).AddTo(_disposables);
            SubmitCommand = new ReactiveCommand(canSubmit, initialCanExecute: false).AddTo(_disposables);

            var canDraw = hand.Cards.Select(cards => cards.Count < hand.MaxHandSize);
            CanDraw = canDraw.ToReadOnlyReactiveProperty(initialValue: true).AddTo(_disposables);
            DrawCommand = new ReactiveCommand(canDraw, initialCanExecute: true).AddTo(_disposables);
        }

        public void PushHandResult(HandResult result)
        {
            _handResultText.Value = ToDisplayString(result.Rank);
        }

        private static string ToDisplayString(HandRank rank) => rank switch
        {
            HandRank.RoyalFlush    => "로열 플러시",
            HandRank.StraightFlush => "스트레이트 플러시",
            HandRank.FourOfAKind   => "포 오브 어 카인드",
            HandRank.FullHouse     => "풀 하우스",
            HandRank.Flush         => "플러시",
            HandRank.Straight      => "스트레이트",
            HandRank.ThreeOfAKind  => "쓰리 오브 어 카인드",
            HandRank.TwoPair       => "투 페어",
            HandRank.OnePair       => "원 페어",
            HandRank.HighCard      => "하이 카드",
            _                      => rank.ToString()
        };

        public void Dispose() => _disposables.Dispose();
    }
}
