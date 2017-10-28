using System.Collections.Generic;
using System.Linq;

namespace IharBury.NHibernate
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Splits a collection in batches with coping elements into a list.
        /// </summary>
        internal static IEnumerable<IList<T>> InBatchesOf<T>(this IList<T> items, int batchSize)
        {
            if (items.Count == 0)
                return Enumerable.Empty<IList<T>>();

            // Avoid creating too large collection when batch size is greater than the number of items.
            if (batchSize >= items.Count)
                return new[] { items };

            return SplitToBatches();

            IEnumerable<IList<T>> SplitToBatches()
            {
                var batch = new List<T>(batchSize);

                foreach (var item in items)
                {
                    batch.Add(item);

                    if (batch.Count >= batchSize)
                    {
                        yield return batch;
                        batch = new List<T>(batchSize);
                    }
                }

                if (batch.Count != 0)
                {
                    yield return batch;
                }
            }
        }
    }
}
