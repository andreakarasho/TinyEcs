using System.Runtime.CompilerServices;

namespace TinyEcs;

public readonly struct Identity
{
	public readonly string Value;

	internal Identity(string value) => Value = value;
}

public static class UniqueEntityPlugin
{
	[ModuleInitializer]
	internal static void ModuleInit()
	{
		World.OnPluginInitialization += world => {
			// initialize the entity component
			world.Component<Identity>();
		};
	}

	public static EntityView Entity(this World world, string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return world.Entity();

		var found = EntityView.Invalid;
		world.Each((EntityView entity, ref Identity identity) => {
			if (identity.Value?.Equals(name) ?? false)
				found = entity;
		});

		if (found.ID != 0)
			return found;

		return world.Entity().Set(new Identity(name));
	}

	public static string Name(this EntityView entity)
	{
		if (!entity.Has<Identity>())
			return $"{entity.ID}";

		return entity.Get<Identity>().Value ?? string.Empty;
	}
}
