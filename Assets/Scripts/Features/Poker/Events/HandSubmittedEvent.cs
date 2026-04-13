using FoldingFate.Features.Card.Models;

namespace FoldingFate.Features.Poker.Events
{
    public readonly struct HandSubmittedEvent
    {
        public HandResult Result { get; }
        public HandSubmittedEvent(HandResult result) { Result = result; }
    }
}