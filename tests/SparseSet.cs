namespace TinyEcs.Tests
{
    public class SparseSetTest
    {
        [Theory]
        [InlineData(1_000_000)]
        public void SparseSet_Add(int amount)
        {
            var set = new EntitySparseSet<EcsComponent>();

            for (var i = 0; i < amount; i++)
            {
                var cmp = new EcsComponent()
                {
                    Size = 123 + i
                };

                var id = set.NewID();
                set.Add(id, cmp);

                Assert.True(set.Contains(id));
                Assert.Equal(cmp, set.Get(id));
            }

            Assert.Equal(amount, set.Length);
        }

        [Fact]
        public void SparseSet_Recycle()
        {
            var set = new EntitySparseSet<EcsComponent>();
            int count = 1000;
            var genCount = 100;
            var ids = new List<ulong>();

            for (int gen = 0; gen < genCount; gen++)
            {
                ids.Clear();

                for (int i = 0; i < count; i++)
                {
                    var id = set.NewID();
                    set.Add(id, default);
                    ids.Add(id);

                    var curGen = IDOp.GetGeneration(id);
                    Assert.Equal(gen, (int)curGen);
                }

                foreach (var id in ids)
                {
                    set.Remove(id);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10_000)]
        [InlineData(1_000_000)]
        public void SparseSet_CheckSequence(int amount)
        {
            var set = new EntitySparseSet<EcsComponent>();

            ulong last = set.NewID();

            for (var i = 0; i < amount; ++i)
            {
                var id = set.NewID();

                var genHi = id & EcsConst.ECS_GENERATION_MASK;
                Assert.Equal(id, last - genHi);

                set.Remove(id);
                last = id;
            }
        }
    }
}
