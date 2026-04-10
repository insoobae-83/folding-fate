using System;
using System.Collections.Generic;
using R3;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class DeckModel
    {
        private readonly List<BaseCard> _cards = new();
        private readonly Random _random = new();

        public ReactiveProperty<int> RemainingCount { get; } = new(0);

        public void Initialize()
        {
            _cards.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new BaseCard(
                        id: $"{suit}_{rank}",
                        category: CardCategory.Standard,
                        suit: suit,
                        rank: rank,
                        displayName: $"{rank} of {suit}s",
                        description: string.Empty));
                }
            }
            Shuffle();
            RemainingCount.Value = _cards.Count;
        }

        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public IReadOnlyList<BaseCard> Draw(int count)
        {
            if (_cards.Count < count)
            {
                Initialize();
            }

            var drawn = _cards.GetRange(0, count);
            _cards.RemoveRange(0, count);
            RemainingCount.Value = _cards.Count;
            return drawn;
        }
    }
}
