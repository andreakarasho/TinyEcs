using TinyEcs.Bevy;

namespace TinyEcs.Tests;

// Entity id 0 is every caller's "nothing" sentinel. GetIter overloads id 0 as
// "no id, iterate the whole query", so the by-id lookups must reject it up front
// — otherwise Contains/Get/TryGet answer with the query's FIRST row and the
// caller acts on an entity it never asked about.
public class QueryEntityZeroTest
{
    private struct Position { public float X, Y; }

    [Fact]
    public void ByIdLookups_RejectEntityZero()
    {
        var app = new App();
        var world = app.GetWorld();
        var real = world.Entity().Set(new Position { X = 7, Y = 9 }).ID;

        var query = new Query<Data<Position>>();
        query.Initialize(app);
        query.Fetch(app);

        Assert.True(query.Contains(real));
        Assert.True(query.TryGet(real, out _));

        Assert.False(query.Contains(0));
        Assert.False(query.TryGet(0, out _));
    }
}
