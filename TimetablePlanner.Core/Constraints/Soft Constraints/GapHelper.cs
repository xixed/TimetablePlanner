using System.Collections.Generic;
using System.Linq;

namespace TimetablePlanner.Core.Constraints.Soft_Constraints
{
    internal static class GapHelper
    {
        public static int CountInternalGaps(IEnumerable<int> periods)
        {
            var ordered = periods.Distinct().OrderBy(p => p).ToList();
            var gaps = 0;

            for (var i = 1; i < ordered.Count; i++)
            {
                var diff = ordered[i] - ordered[i - 1];
                if (diff > 1)
                {
                    gaps += diff - 1;
                }
            }

            return gaps;
        }
    }
}