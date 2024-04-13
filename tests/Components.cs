using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TinyEcs.Tests;

unsafe struct LargeComponent
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

struct FloatComponent
{
    public float Value;
}

struct IntComponent
{
    public int Value;
}

struct BoolComponent
{
    public bool Value;
}

struct NormalTag { }

public class ComponentTest
{
    [Fact]
    public void Check_Validate_Tag()
    {
        using var ctx = new Context();
        ref readonly var cmp = ref ctx.World.Component<NormalTag>();

        Assert.Equal(0, cmp.Size);
    }

    [Fact]
    public unsafe void Check_Validate_Component()
    {
        using var ctx = new Context();
        ref readonly var cmp = ref ctx.World.Component<FloatComponent>();

        Assert.Equal(sizeof(FloatComponent), cmp.Size);
    }
}
