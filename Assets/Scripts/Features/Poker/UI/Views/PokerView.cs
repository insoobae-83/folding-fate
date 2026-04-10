using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using FoldingFate.Core;
using FoldingFate.Features.Card.Models;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.UI.Views
{
    [RequireComponent(typeof(UIDocument))]
    public class PokerView : MonoBehaviour
    {
        [Inject] private PokerViewModel _vm;
        [Inject] private PokerConfig _config;

        private UIDocument _doc;
        private VisualElement _handContainer;
        private Label _resultLabel;
        private Label _deckCountLabel;
        private Button _submitButton;
        private Button _discardButton;
        private VisualElement _showcaseContainer;
        private VisualElement _pokerRoot;
        private VisualElement _deckStack;

        private readonly List<VisualElement> _cardElements = new();
        private readonly List<VisualElement> _cardOverlays = new();

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = _doc.rootVisualElement;
            _pokerRoot = root.Q("poker-root");
            _handContainer = root.Q("hand-container");
            _showcaseContainer = root.Q("showcase-container");
            _resultLabel = root.Q<Label>("result-label");
            _deckCountLabel = root.Q<Label>("deck-count-label");
            _submitButton = root.Q<Button>("submit-button");
            _discardButton = root.Q<Button>("discard-button");
            _deckStack = root.Q("deck-stack");

            _submitButton.clicked += () => _vm.SubmitCommand.Execute(Unit.Default);
            _discardButton.clicked += () => _vm.DiscardCommand.Execute(Unit.Default);

            _vm.Hand.Subscribe(RenderHand).AddTo(this);
            _vm.SelectedIndices.Subscribe(UpdateSelectionVisuals).AddTo(this);
            _vm.HandResultText.Subscribe(text => _resultLabel.text = text).AddTo(this);
            _vm.DeckRemaining.Subscribe(count => _deckCountLabel.text = $"남은 카드: {count}").AddTo(this);
            _vm.Showcase.Subscribe(RenderShowcase).AddTo(this);

            _vm.CanSubmit.Subscribe(v => _submitButton.SetEnabled(v)).AddTo(this);
            _vm.CanSubmit.Subscribe(v => _discardButton.SetEnabled(v)).AddTo(this);
        }

        private void RenderHand(IReadOnlyList<BaseCard> cards)
        {
            int prevCount = _cardElements.Count;
            bool isDealing = _vm.IsDealing.CurrentValue;

            _handContainer.Clear();
            _cardElements.Clear();
            _cardOverlays.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                int capturedIndex = i;
                var card = cards[i];
                var (cardEl, overlay) = CreateCardElement(card, capturedIndex);

                if (isDealing && i >= prevCount)
                {
                    AnimateDealCard(cardEl).Forget();
                }

                _handContainer.Add(cardEl);
                _cardElements.Add(cardEl);
                _cardOverlays.Add(overlay);
            }
        }

        private async UniTaskVoid AnimateDealCard(VisualElement cardEl)
        {
            if (_deckStack == null) return;

            var deckRect = _deckStack.worldBound;
            var handRect = _handContainer.worldBound;

            float offsetX = deckRect.x - handRect.x - (_cardElements.Count - 1) * 88f;
            float offsetY = deckRect.y - handRect.y;

            cardEl.style.translate = new Translate(offsetX, offsetY);
            cardEl.AddToClassList("card--dealing");

            await UniTask.Yield(PlayerLoopTiming.Update, this.destroyCancellationToken);

            cardEl.style.transitionDuration = new List<TimeValue> { new(
                _config.DealAnimationDurationSeconds, TimeUnit.Second) };
            cardEl.style.translate = new Translate(0, 0);

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(_config.DealAnimationDurationSeconds),
                cancellationToken: this.destroyCancellationToken);

            cardEl.RemoveFromClassList("card--dealing");
            cardEl.style.translate = StyleKeyword.Null;
            cardEl.style.transitionDuration = StyleKeyword.Null;
        }

        private (VisualElement card, VisualElement overlay) CreateCardElement(BaseCard card, int index)
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

            var overlay = new VisualElement();
            overlay.AddToClassList("card-overlay");

            el.Add(rankLabel);
            el.Add(suitTopLabel);
            el.Add(centerSuitLabel);
            el.Add(overlay);

            el.RegisterCallback<ClickEvent>(_ => _vm.ToggleSelectCommand.Execute(index));

            return (el, overlay);
        }

        private void UpdateSelectionVisuals(IReadOnlyList<int> selectedIndices)
        {
            var selectedSet = new HashSet<int>(selectedIndices);
            for (int i = 0; i < _cardElements.Count; i++)
            {
                bool selected = selectedSet.Contains(i);
                if (selected)
                {
                    _cardElements[i].AddToClassList("card--selected");
                    _cardOverlays[i].AddToClassList("card-overlay--selected");
                }
                else
                {
                    _cardElements[i].RemoveFromClassList("card--selected");
                    _cardOverlays[i].RemoveFromClassList("card-overlay--selected");
                }
            }
        }

        private void RenderShowcase(ShowcaseState state)
        {
            _showcaseContainer.Clear();

            if (state.IsActive)
            {
                _showcaseContainer.AddToClassList("showcase-container--active");
                _pokerRoot.AddToClassList("poker-root--showcasing");

                var cardsRow = new VisualElement();
                cardsRow.AddToClassList("showcase-cards");

                foreach (var card in state.Cards)
                {
                    var cardEl = CreateShowcaseCardElement(card, state.IsHighlighted(card));
                    cardsRow.Add(cardEl);
                }

                var rankLabel = new Label();
                rankLabel.AddToClassList("showcase-rank-text");
                rankLabel.text = state.RankText;

                _showcaseContainer.Add(cardsRow);
                _showcaseContainer.Add(rankLabel);
            }
            else
            {
                _showcaseContainer.RemoveFromClassList("showcase-container--active");
                _pokerRoot.RemoveFromClassList("poker-root--showcasing");
            }
        }

        private VisualElement CreateShowcaseCardElement(BaseCard card, bool isHighlighted)
        {
            var el = new VisualElement();
            el.AddToClassList(isHighlighted ? "showcase-card" : "showcase-card--dimmed");

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

            if (isHighlighted)
            {
                var overlay = new VisualElement();
                overlay.AddToClassList("showcase-overlay");
                el.Add(overlay);
            }

            return el;
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
