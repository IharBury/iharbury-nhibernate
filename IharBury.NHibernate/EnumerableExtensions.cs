using System;
using System.Collections.Generic;

namespace IharBury.NHibernate
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Splits a collection in batches with coping elements into a list.
        /// </summary>
        /// <exception cref="ArgumentNullException">When <paramref name="items"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="batchSize"/> is not positive.</exception>
        internal static IEnumerable<IList<T>> InBatchesOf<T>(this IList<T> items, int batchSize)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (batchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Must be positive.");

            // Arguments are validated before the iterator to throw before the enumeration.
            return SplitIntoBatches();

            IEnumerable<IList<T>> SplitIntoBatches()
            {
                var fullBatchCount = items.Count / batchSize;
                var currentItemIndex = 0;

                for (var currentBatchIndex = 0; currentBatchIndex < fullBatchCount; currentBatchIndex++)
                {
                    var batch = new T[batchSize];
                    var currentItemInBatchIndex = 0;

                    while (currentItemInBatchIndex < batchSize)
                    {
                        batch[currentItemInBatchIndex] = items[currentItemIndex];
                        currentItemInBatchIndex++;
                        currentItemIndex++;
                    }

                    yield return batch;
                }

                if (currentItemIndex < items.Count)
                {
                    var lastBatchSize = items.Count - currentItemIndex;
                    var batch = new T[lastBatchSize];
                    var currentItemInBatchIndex = 0;

                    while (currentItemInBatchIndex < lastBatchSize)
                    {
                        batch[currentItemInBatchIndex] = items[currentItemIndex];
                        currentItemInBatchIndex++;
                        currentItemIndex++;
                    }

                    yield return batch;
                }
            }
        }
    }
}
