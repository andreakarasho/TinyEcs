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
            var set = new SparseSet<int>(32);

            for (int i = 0; i < amount;i++)
            {
                set.Add(i + 0x4000_0000, 12312);
                Assert.True(set.Contains(i + 0x4000_0000));
                Assert.Equal(12312, set[i + 0x4000_0000]);
            }

            Assert.Equal(amount, set.Length);
        }
    }
}
