using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.UI.Views
{
    [RequireComponent(typeof(UIDocument))]
    public class PokerView : MonoBehaviour
    {
        [Inject] private PokerViewModel _vm;

        private UIDocument _doc;
        private VisualElement _handContainer;
        private Label _resultLabel;
        private Label _deckCountLabel;
        private Button _submitButton;
        private Button _drawButton;

        private readonly List<VisualElement> _cardElements = new();

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = _doc.rootVisualElement;
            _handContainer = root.Q("hand-container");
            _resultLabel = root.Q<Label>("result-label");
            _deckCountLabel = root.Q<Label>("deck-count-label");
            _submitButton = root.Q<Button>("submit-button");
            _drawButton = root.Q<Button>("draw-button");

            _submitButton.clicked += () => _vm.SubmitCommand.Execute();
            _drawButton.clicked += () => _vm.DrawCommand.Execute();

            var token = destroyCancellationToken;

            _vm.Hand.Subscribe(RenderHand).AddTo(token);
            _vm.SelectedIndices.Subscribe(UpdateSelectionVisuals).AddTo(token);
            _vm.HandResultText.Subscribe(text => _resultLabel.text = text).AddTo(token);
            _vm.DeckRemaining.Subscribe(count => _deckCountLabel.text = $"남은 카드: {count}").AddTo(token);

            _vm.SubmitCommand.CanExecute
                .Subscribe(v => _submitButton.SetEnabled(v)).AddTo(token);
            _vm.DrawCommand.CanExecute
                .Subscribe(v => _drawButton.SetEnabled(v)).AddTo(token);
        }

        private void RenderHand(IReadOnlyList<BaseCard> cards)
        {
            _handContainer.Clear();
            _cardElements.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                int capturedIndex = i;
                var card = cards[i];
                var cardEl = CreateCardElement(card, capturedIndex);
                _handContainer.Add(cardEl);
                _cardElements.Add(cardEl);
            }

        }

        private VisualElement CreateCardElement(BaseCard card, int index)
        {
            var el = new VisualElement();
            el.AddToClassList("card");

            bool isRed = card.Suit == Suit.Heart || card.Suit == Suit.Diamond;
            el.AddToClassList(isRed ? "card--red" : "card--black");

            var rankLabel = new Label { name = "card-top-rank" };
            rankLabel.AddToClassList("card-rank");
            rankLabel.text = RankToDisplay(card.Rank);

            var suitTopLabel = new Label { name = "card-suit" };
            suitTopLabel.AddToClassList("card-suit--top");
            suitTopLabel.text = SuitToSymbol(card.Suit);

            var centerSuitLabel = new Label { name = "card-center-suit" };
            centerSuitLabel.AddToClassList("card-center-suit");
            centerSuitLabel.text = SuitToSymbol(card.Suit);

            el.Add(rankLabel);
            el.Add(suitTopLabel);
            el.Add(centerSuitLabel);

            el.RegisterCallback<ClickEvent>(_ => _vm.ToggleSelectCommand.Execute(index));

            return el;
        }

        private void UpdateSelectionVisuals(IReadOnlyList<int> selectedIndices)
        {
            var selectedSet = new HashSet<int>(selectedIndices);
            for (int i = 0; i < _cardElements.Count; i++)
            {
                if (selectedSet.Contains(i))
                    _cardElements[i].AddToClassList("card--selected");
                else
                    _cardElements[i].RemoveFromClassList("card--selected");
            }
        }

        private static string SuitToSymbol(Suit? suit) => suit switch
        {
            Suit.Spade   => "♠",
            Suit.Heart   => "♥",
            Suit.Diamond => "♦",
            Suit.Club    => "♣",
            _            => "?"
        };

        private static string RankToDisplay(Rank? rank) => rank switch
        {
            Rank.Ace   => "A",
            Rank.Jack  => "J",
            Rank.Queen => "Q",
            Rank.King  => "K",
            null       => "?",
            _          => ((int)rank.Value).ToString()
        };
    }
}
