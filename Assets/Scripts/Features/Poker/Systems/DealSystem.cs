using System.Linq;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Models;

namespace FoldingFate.Features.Poker.Systems
{
    public class DealSystem
    {
        private readonly DeckModel _deck;
        private readonly HandModel _hand;
        private readonly HandEvaluator _evaluator;

        public DealSystem(DeckModel deck, HandModel hand, HandEvaluator evaluator)
        {
            _deck = deck;
            _hand = hand;
            _evaluator = evaluator;
        }

        public void InitializeDeck() => _deck.Initialize();

        public void Deal(int count)
        {
            var drawn = _deck.Draw(count);
            _hand.AddCards(drawn);
        }

        public void ToggleSelect(int index)
        {
            if (index < 0 || index >= _hand.Cards.Value.Count) return;
            bool isSelected = _hand.SelectedIndices.Value.Contains(index);
            if (!isSelected && _hand.SelectedCount >= 5) return;
            _hand.ToggleSelect(index);
        }

        public HandResult EvaluateSelected()
        {
            var cards = _hand.Cards.Value;
            var selectedCards = _hand.SelectedIndices.Value
                .Select(i => cards[i])
                .ToList();
            return _evaluator.Evaluate(selectedCards);
        }

        public void DiscardSelected() => _hand.RemoveSelected();

        public void DrawToFull()
        {
            int needed = _hand.MaxHandSize - _hand.Cards.Value.Count;
            if (needed <= 0) return;
            var drawn = _deck.Draw(needed);
            _hand.AddCards(drawn);
        }
    }
}
