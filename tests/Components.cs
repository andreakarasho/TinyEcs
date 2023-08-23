using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TinyEcs.Tests;

unsafe struct LargeComponent : IComponent
{
    const int SIZE = 1024;
    private fixed float _array[SIZE];

    public Span<float> Span
    {
        get
        {
            fixed (float* ptr = _array)
            {
                return new Span<float>(ptr, SIZE);
            }
        }
    }
}

struct FloatComponent : IComponent
{
	public float Value;
}

struct IntComponent : IComponent
{
	public int Value;
}

struct BoolComponent : IComponent
{
	public bool Value;
}

struct NormalTag : ITag { }


public class ComponentTest
{
	[Fact]
	public void Check_Validate_Tag<TContext>()
	{
		using var world = new World<TContext>();
		ref var cmp = ref world.Component<NormalTag>();

		Assert.Equal(0, cmp.Size);
		Assert.True(world.Has<EcsTag>(cmp.ID));
	}

	[Fact]
	public unsafe void Check_Validate_Component<TContext>()
	{
		using var world = new World<TContext>();
		ref var cmp = ref world.Component<FloatComponent>();

		Assert.Equal(sizeof(FloatComponent), cmp.Size);
		Assert.False(world.Has<EcsTag>(cmp.ID));
	}

	[Fact]
	public unsafe void Check_Validate_Pair<TContext>()
	{
		using var world = new World<TContext>();
		var id = world.Pair<NormalTag, FloatComponent>();

		Assert.Equal(0, world.Component<NormalTag>().Size);
		Assert.Equal(sizeof(FloatComponent), world.Component<FloatComponent>().Size);
	}
}
