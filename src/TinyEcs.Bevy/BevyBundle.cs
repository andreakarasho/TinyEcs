using System;

namespace TinyEcs.Bevy;

/// <summary>
/// A Bundle is a collection of components that can be inserted together.
/// Bevy-style bundles allow grouping related components for convenient entity creation.
/// </summary>
public interface IBundle
{
	/// <summary>
	/// Insert all components in this bundle into the entity (immediate mode).
	/// </summary>
	void Insert(EntityView entity);

	/// <summary>
	/// Insert all components in this bundle into the entity (deferred mode).
	/// </summary>
	void Insert(EntityCommands entity);
}

/// <summary>
/// Extension methods for working with Bundles
/// </summary>
public static class BundleExtensions
{
	/// <summary>
	/// Insert a bundle of components into this entity
	/// </summary>
	public static EntityView InsertBundle<TBundle>(this EntityView entity, TBundle bundle)
		where TBundle : struct, IBundle
	{
		bundle.Insert(entity);
		return entity;
	}

	/// <summary>
	/// Insert a bundle of components into this entity (deferred)
	/// </summary>
	public static EntityCommands InsertBundle<TBundle>(this EntityCommands entity, TBundle bundle)
		where TBundle : struct, IBundle
	{
		bundle.Insert(entity);
		return entity;
	}

	/// <summary>
	/// Spawn an entity with a bundle of components
	/// </summary>
	public static EntityCommands SpawnBundle<TBundle>(this Commands commands, TBundle bundle)
		where TBundle : struct, IBundle
	{
		var entity = commands.Spawn();
		bundle.Insert(entity);
		return entity;
	}
}
