using System;
using FoldingFate.Core;

namespace FoldingFate.Features.Battle.Models
{
    public class ActionResult
    {
        public BattleAction Source { get; }
        public ActionResultType ResultType { get; }
        public FoldingFate.Core.Entity Target { get; }
        public float Value { get; }

        public ActionResult(BattleAction source, ActionResultType resultType, FoldingFate.Core.Entity target, float value)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ResultType = resultType;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Value = value;
        }
    }
}
