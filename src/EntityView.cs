namespace TinyEcs;

/// <summary>
/// A wrapper around an EcsID which contains shortcuts methods.
/// </summary>
#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[DebuggerDisplay("ID: {ID}")]
public readonly struct EntityView : IEquatable<EcsID>, IEquatable<EntityView>
{
    public static readonly EntityView Invalid = new(null!, 0);


	/// <inheritdoc cref="EcsID"/>
    public readonly EcsID ID;

	/// <inheritdoc cref="TinyEcs.World"/>
    public readonly World World;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityView(World world, EcsID id)
    {
        World = world;
        ID = id;
    }

	/// <inheritdoc cref="EcsID.Generation"/>
	public readonly int Generation => ID.Generation();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EcsID other)
		=> ID == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EntityView other)
		=> ID == other.ID;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override int GetHashCode()
		=> ID.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override bool Equals(object? obj)
		=> obj is EntityView ent && Equals(ent);



	/// <inheritdoc cref="World.Add{T}(EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Add<T>() where T : struct
	{
        World.Add<T>(ID);
        return this;
    }

	/// <inheritdoc cref="World.Set{T}(EcsID, T)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Set<T>(T component) where T : struct
	{
        World.Set(ID, component);
        return this;
    }

	/// <inheritdoc cref="World.Add(EcsID, EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Add(EcsID id)
	{
		World.Add(ID, id);
		return this;
	}

	/// <inheritdoc cref="World.Add(EcsID, EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Add(EntityView id)
		=> Add(id.ID);

	/// <inheritdoc cref="World.Unset{T}(EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset<T>() where T : struct
	{
        World.Unset<T>(ID);
        return this;
    }

	/// <inheritdoc cref="World.Unset(EcsID, EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset(EcsID id)
	{
        World.Unset(ID, id);
        return this;
    }

	/// <inheritdoc cref="World.Unset(EcsID, EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset(EntityView id)
		=> Unset(id.ID);

	/// <inheritdoc cref="World.GetType"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<ComponentInfo> Type()
		=> World.GetType(ID);

	/// <inheritdoc cref="World.Get{T}(EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>() where T : struct
		=> ref World.Get<T>(ID);

	/// <inheritdoc cref="World.Has{T}(EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has<T>() where T : struct
		=> World.Has<T>(ID);

	/// <inheritdoc cref="World.Has(EcsID, EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has(EcsID id)
		=> World.Has(ID, id);

	/// <inheritdoc cref="World.Has(EcsID, EcsID)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has(EntityView id)
		=> World.Has(ID, id.ID);

	/// <inheritdoc cref="World.Delete"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Delete()
		=> World.Delete(ID);

	/// <inheritdoc cref="World.Exists"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Exists()
		=> World.Exists(ID);

#if USE_PAIR
	/// <inheritdoc cref="World.Rule{TRule}"/>
	public readonly EntityView Rule<TRule>() where TRule : struct
	{
		World.Rule<TRule>(ID);
		return this;
	}

	/// <inheritdoc cref="World.Rule(EcsID, EcsID)"/>
	public readonly EntityView Rule(EcsID ruleId)
	{
		World.Rule(ID, ruleId);
		return this;
	}
#endif

	public static implicit operator EcsID(EntityView d) => d.ID;

    public static bool operator ==(EntityView a, EntityView b) => a.ID.Equals(b.ID);
    public static bool operator !=(EntityView a, EntityView b) => !(a == b);
}
