namespace TinyEcs;

public static class Defaults
{
	/// <summary>
	/// Wildcard is used to specify "any component/tag".<br/>It's mostly used for queries.
	/// </summary>
	public readonly struct Wildcard { public static readonly EcsID ID = Lookup.Component<Wildcard>.Value.ID; }

#if USE_PAIR
	/// <summary>
	/// Built-in tag.<br/>Shortcut for child.Add{ChildOf}(parent);
	/// </summary>
	public readonly struct ChildOf { public static readonly EcsID ID = Lookup.Component<ChildOf>.Value.ID; }

	/// <summary>
	/// Built-in component.<br/>Used in combination with <see cref="Name"/>
	/// </summary>
	public readonly struct Identifier
	{
		public static readonly EcsID ID = Lookup.Component<Identifier>.Value.ID;

		internal Identifier(string name) => Data = name;

		public readonly string Data;
	}

	/// <summary>
	/// Built-in tag.<br/>Used in combination with <see cref="Identifier"/>
	/// </summary>
	public readonly struct Name
	{
		public static readonly EcsID ID = Lookup.Component<Name>.Value.ID;
	}

    /// <summary>
    /// Built-in tag.<br/>Used to make a component/tag unique when adding a Pair.<para/>
	/// <example>
	/// 	Example:
	/// <code>
	///     var alice = world.Entity("Alice");
	///     var eats = world.Entity("Eats").Add&lt;Unique&gt;();
	///     var pasta = world.Entity("Pasta");
	///     var apple = world.Entity("Apple");
	///
	///     alice.Add(eats, pasta);
	///     alice.Add(eats, apple);
	///
	///     Assert.False(alice.Has(eats, pasta)):
	///     Assert.True(alice.Has(eats, apple)):
	/// </code>
	/// </example>
    /// </summary>
	public readonly struct Unique { public static readonly EcsID ID = Lookup.Component<Unique>.Value.ID; }

    /// <summary>
    /// Built-in tag.<br/>Used to add the same rule to the target entity.<para/>
	/// <example>
	/// 	Example:
	/// <code>
	///		var carl = world.Entity();
	///		var tradeWith = world.Entity().Add&lt;Symmetric&gt;();
	///		var bob = world.Entity();
	///
	/// 	// carl trades with bob
	///		carl.Add(tradeWith, bob);
	///		// now also bob trades with carl
	///
	///		Assert.True(carl.Has(tradeWith, bob));
	///		Assert.True(bob.Has(tradeWith, carl));
	/// </code>
	/// </example>
    /// </summary>
	public readonly struct Symmetric { public static readonly EcsID ID = Lookup.Component<Symmetric>.Value.ID; }

	/// <summary>
    /// Built-in tag.<br/>Mark a component/tag to not be deleted<para/>
	/// <example>
	/// 	Example:
	/// <code>
	///		var youCantDeleteMe = world.Entity().Add&lt;DoNotDelete&gt;();
	///		var reallyImp = world.Entity&lt;ReallyImportant&gt;().Add&lt;DoNotDelete&gt;();
	///
	///		youCantDeleteMe.Delete(); 	// --> runtime error
	///		reallyImp.Delete(); 		// --> runtime error
	///
	/// 	struct ReallyImportant { }
	/// </code>
	/// </example>
    /// </summary>
	public struct DoNotDelete { }

	/// <summary>
    /// Built-in tag.<br/>Cleanup rule<para/>
	/// <example>
	/// 	Example:
	/// <code>
	/// 	var a = world.Entity();
	///		var relation = world.Entity().Add&lt;OnDelete, Delete&gt;();
	///		var b = world.Entity();
	///
	///		a.Add(relation, b);
	///		b.Delete(); // --> a get deleted too
	/// </code>
	/// </example>
    /// </summary>
	public struct OnDelete { }


	/// <summary>
    /// Built-in tag.<br/>Cleanup rule for Delete<para/>
	/// <example>
	/// 	Example:
	/// <code>
	/// 	var a = world.Entity();
	///		var relation = world.Entity().Add&lt;OnDelete, Delete&gt;();
	///		var b = world.Entity();
	///
	///		a.Add(relation, b);
	///		b.Delete(); // --> 'a' get deleted too
	/// </code>
	/// </example>
    /// </summary>
	public struct Delete { }

	/// <summary>
    /// Built-in tag.<br/>Cleanup rule for Delete<para/>
	/// <example>
	/// 	Example:
	/// <code>
	/// 	var a = world.Entity();
	///		var relation = world.Entity().Add&lt;OnDelete, Panic&gt;();
	///		var b = world.Entity();
	///
	///		a.Add(relation, b);
	///		b.Delete(); // --> runtime error!
	/// </code>
	/// </example>
    /// </summary>
	public struct Panic { }


	/// <summary>
    /// Built-in tag.<br/>Cleanup rule for Delete<para/>
	/// <example>
	/// 	Example:
	/// <code>
	/// 	var a = world.Entity();
	///		var relation = world.Entity().Add&lt;OnDelete, Unset&gt;();
	///		var b = world.Entity();
	///
	///		a.Add(relation, b);
	///		b.Delete(); // --> relation get removed from 'a'
	/// </code>
	/// </example>
    /// </summary>
	public readonly struct Unset { public static readonly EcsID ID = Lookup.Component<Unset>.Value.ID; }




	internal readonly struct Rule { public static readonly EcsID ID = Lookup.Component<Rule>.Value.ID; }
#endif
}
