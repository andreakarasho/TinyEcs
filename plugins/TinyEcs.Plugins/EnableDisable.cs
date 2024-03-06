using System.Runtime.CompilerServices;

namespace TinyEcs;

public readonly struct Disabled {}

public static class EnableDisable
{
	[ModuleInitializer]
	internal static void ModuleInit()
	{
		World.OnPluginInitialization += world => {
			world.Component<Disabled>();
		};
	}

    public static EntityView Enable(this EntityView entity)
		=> entity.Unset<Disabled>();

    public static EntityView Disable(this EntityView entity)
		=> entity.Set<Disabled>();

    public static bool IsEnabled(this EntityView entity)
		=> !entity.Has<Disabled>();

	public static void Enable(this World world, EcsID id)
		=> world.Unset<Disabled>(id);

	public static void Disable(this World world, EcsID id)
		=> world.Set<Disabled>(id);

	public static bool IsEnabled(this World world, EcsID id)
		=> !world.Has<Disabled>(id);
}
