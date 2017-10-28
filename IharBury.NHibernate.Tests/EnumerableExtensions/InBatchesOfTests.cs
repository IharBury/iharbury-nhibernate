using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IharBury.NHibernate.Tests.EnumerableExtensions
{
    public sealed class InBatchesOfTests
    {
        [Fact]
        public void EmptyCollectionSplitsIntoZeroBatches()
        {
            var collection = new string[] { };
            Assert.Empty(collection.InBatchesOf(10));
        }

        [Fact]
        public void FiveItemsAreSplitIntoThreeBatchesOfTwoItems()
        {
            var collection = Enumerable.Range(1, 5).ToList();
            var batches = collection.InBatchesOf(2).ToList();
            Assert.Equal(3, batches.Count);
            Assert.Equal(1, batches[0][0]);
            Assert.Equal(2, batches[0][1]);
            Assert.Equal(3, batches[1][0]);
            Assert.Equal(4, batches[1][1]);
            Assert.Equal(5, batches[2][0]);
        }

        [Fact]
        public void BatchesDoNotHaveExcessiveCapacity()
        {
            var collection = Enumerable.Range(1, 5).ToList();
            var batches = collection.InBatchesOf(2).ToList();

            // Arrays have capacity equal to their size.
            Assert.All(batches, batch => Assert.IsType<int[]>(batch));
        }
    }
}
