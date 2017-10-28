using System.Collections.Generic;

namespace IharBury.NHibernate
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Splits a collection in batches with coping elements into a list.
        /// </summary>
        internal static IEnumerable<IList<T>> InBatchesOf<T>(this IList<T> items, int batchSize)
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
