namespace FoldingFate.Features.Battle.Events
{
    public readonly struct BattleTurnCompleteEvent
    {
        public bool IsBattleOver { get; }

        public BattleTurnCompleteEvent(bool isBattleOver)
        {
            IsBattleOver = isBattleOver;
        }
    }
}
