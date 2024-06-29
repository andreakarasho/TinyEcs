namespace TinyEcs;

public static class Defaults
{
	/// <summary>
	/// Wildcard is used to specify "any component/tag".<br/>It's mostly used for queries.
	/// </summary>
	public struct Wildcard { public static EcsID ID = Lookup.Component<Wildcard>.Value.ID; }

	/// <summary>
	/// Built-in tag.<br/>Shortcut for child.Add{ChildOf}(parent);
	/// </summary>
	public struct ChildOf { }

	/// <summary>
	/// Built-in tag.<br/>Used in combination with <see cref="Name"/>
	/// </summary>
	public struct Identifier { }

	/// <summary>
	/// Built-in tag.<br/>Used in combination with <see cref="Identifier"/>
	/// </summary>
	public readonly struct Name { internal Name(string value) => Value = value; public readonly string Value; }

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
	public struct Unique { }

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
	public struct Symmetric { }

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
}
