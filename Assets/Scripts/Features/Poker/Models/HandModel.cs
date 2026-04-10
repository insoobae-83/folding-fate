using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Models
{
    public class HandModel : IDisposable
    {
        private readonly List<BaseCard> _cards = new();
        private readonly HashSet<int> _selectedIndices = new();

        public int MaxHandSize { get; }
        public ReactiveProperty<IReadOnlyList<BaseCard>> Cards { get; } = new(new List<BaseCard>());
        public ReactiveProperty<IReadOnlyList<int>> SelectedIndices { get; } = new(new List<int>());

        public int SelectedCount => _selectedIndices.Count;
        /// <summary>Advisory: returns true when card count has reached MaxHandSize. Size enforcement is the caller's responsibility.</summary>
        public bool IsFull => _cards.Count >= MaxHandSize;

        public HandModel(int maxHandSize = 8)
        {
            MaxHandSize = maxHandSize;
        }

        public void AddCards(IReadOnlyList<BaseCard> cards)
        {
            _cards.AddRange(cards);
            Cards.Value = _cards.ToList();
        }

        public void ToggleSelect(int index)
        {
            if (index < 0 || index >= _cards.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_selectedIndices.Contains(index))
                _selectedIndices.Remove(index);
            else
                _selectedIndices.Add(index);

            SelectedIndices.Value = _selectedIndices.OrderBy(i => i).ToList();
        }

        public void Clear()
        {
            _cards.Clear();
            _selectedIndices.Clear();
            Cards.Value = new List<BaseCard>();
            SelectedIndices.Value = new List<int>();
        }

        public void Dispose()
        {
            Cards.Dispose();
            SelectedIndices.Dispose();
        }
    }
}
