using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyEcs.Tests
{
    public class SparseSetTest
    {
        [Theory]
        [InlineData(1_000_000)]
        public void SparseSet_Add(int amount)
        {
            var set = new SparseSet();

            for (int i = 0; i < amount;i++)
            {
                set.Add(i);
                Assert.True(set.Has(i));
            }

            Assert.Equal(amount, set.Count);
        }
    }
}
