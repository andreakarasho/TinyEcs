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
