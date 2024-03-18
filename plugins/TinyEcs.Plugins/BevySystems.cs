// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using TinyEcs;

// namespace BevySystems;

// // https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

// // public interface ISystemParam
// // {
// //     //object Retrieve(Dictionary<Type, ISystemParam> resources);
// // }

// public sealed class Res<T> : ISystemParam
// {
//     public T Value { get; set; }
// }

// public interface ISystem
// {
//     void Run(Dictionary<Type, ISystemParam> resources);
// }

// public sealed class ErasedFunctionSystem : ISystem
// {
//     private readonly Action<Dictionary<Type, ISystemParam>> f;

//     public ErasedFunctionSystem(Action<Dictionary<Type, ISystemParam>> f)
//     {
//         this.f = f;
//     }

//     public void Run(Dictionary<Type, ISystemParam> resources) => f(resources);
// }

// public sealed partial class Scheduler
// {
//     private readonly List<ISystem> _systems = new ();
//     private readonly Dictionary<Type, ISystemParam> _resources = new ();

//     public void Run()
//     {
//         foreach (var system in _systems)
//         {
//             system.Run(_resources);
//         }
//     }

//     public void AddSystem<T0>(Action<T0> system) where T0 : ISystemParam
//     {
// 		var fn = (Dictionary<Type, ISystemParam> res) => system((T0)res[typeof(T0)]);
// 		_systems.Add(new ErasedFunctionSystem(fn));
//     }

//     public void AddSystem<T0, T1>(Action<T0, T1> system) where T0 : ISystemParam where T1 : ISystemParam
//     {
// 		var fn = (Dictionary<Type, ISystemParam> res) => system((T0)res[typeof(T0)], (T1)res[typeof(T1)]);
// 		_systems.Add(new ErasedFunctionSystem(fn));
//     }

// 	public void AddSystem<T0, T1, T2>(Action<T0, T1, T2> system) where T0 : ISystemParam where T1 : ISystemParam where T2 : ISystemParam
//     {
// 		var fn = (Dictionary<Type, ISystemParam> res) => system((T0)res[typeof(T0)], (T1)res[typeof(T1)], (T2)res[typeof(T2)]);
// 		_systems.Add(new ErasedFunctionSystem(fn));
//     }


//     public void AddResource<T>(T resource)
//     {
//         _resources[typeof(Res<T>)] = new Res<T>() { Value = resource };
//     }

// 	public void AddSystemParam<T>(T param) where T : ISystemParam
// 	{
// 		_resources[typeof(T)] = param;
// 	}
// }
