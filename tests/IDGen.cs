namespace TinyEcs.Tests
{
    public class IDGenTest
    {
        [Fact]
        public void ID_CheckComponent()
        {
            for (int i = 1; i < 5_000_000; i++)
            {
                var id = (ulong)i;
                var cmdID = IDOp.SetAsComponent(id);

                Assert.True(IDOp.IsComponent(cmdID), $"{i} is not a component");
                Assert.Equal(id, IDOp.RealID(cmdID));
            }
        }
    }
}
