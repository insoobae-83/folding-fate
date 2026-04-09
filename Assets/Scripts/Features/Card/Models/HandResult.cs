using System;
using System.Collections.Generic;
using FoldingFate.Core;

namespace FoldingFate.Features.Card.Models
{
    public class HandResult : IComparable<HandResult>
    {
        public HandRank Rank { get; }
        public IReadOnlyList<BaseCard> BestHand { get; }
        private readonly IReadOnlyList<int> _tiebreakValues;

        public HandResult(HandRank rank, List<BaseCard> bestHand, List<int> tiebreakValues)
        {
            Rank = rank;
            BestHand = bestHand.AsReadOnly();
            _tiebreakValues = tiebreakValues.AsReadOnly();
        }

        public int CompareTo(HandResult other)
        {
            if (other == null) return 1;
            int rankCmp = Rank.CompareTo(other.Rank);
            if (rankCmp != 0) return rankCmp;
            for (int i = 0; i < Math.Min(_tiebreakValues.Count, other._tiebreakValues.Count); i++)
            {
                int cmp = _tiebreakValues[i].CompareTo(other._tiebreakValues[i]);
                if (cmp != 0) return cmp;
            }
            return 0;
        }
    }
}
