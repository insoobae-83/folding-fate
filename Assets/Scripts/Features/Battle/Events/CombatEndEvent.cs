namespace FoldingFate.Features.Battle.Events
{
    public readonly struct CombatEndEvent
    {
        public string BattleId { get; }
        public bool IsVictory { get; }

        public CombatEndEvent(string battleId, bool isVictory)
        {
            BattleId = battleId;
            IsVictory = isVictory;
        }
    }
}
