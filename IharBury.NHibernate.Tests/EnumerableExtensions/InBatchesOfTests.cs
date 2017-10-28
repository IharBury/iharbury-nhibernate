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
    }
}
