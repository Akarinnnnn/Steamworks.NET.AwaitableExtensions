// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2025-2025 Akainnnn
// Please see the included LICENSE.txt for additional information.

// This file is provided as a sample, copy into your project if you need.


#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if UNITY_2022_3_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
// I(Akarinnnn) tested that Unity 2022.3 using C# 9, which have NRT
#define STEAMWORKS_SDK_FEATURE_NULLABLE
#nullable enable
#endif

#if !DISABLESTEAMWORKS

using System;
using System.Threading;

namespace Steamworks.NET.AwaitableExtensions
{
	/// <summary>
	/// 
	/// </summary>
	public static class CallResultExtensions
	{
		/// <summary>
		/// Convert call-result handle to a Task like wrapper, to be <see langword="await" />.
		/// <see cref="SynchronizationContext.Current"/> and <see cref="AsyncLocal{T}"/> are not respected by default.
		/// </summary>
		/// <example>
		/// await ToTask&lt;SteamUGCQueryCompleted_t&gt;(handle, cancellationToken);
		/// </example>
		/// <typeparam name="T"></typeparam>
		/// <param name="handle">Steam call-result handle will be associated to this awaiting.</param>
		/// <param name="cancellationToken">Cancelation token used to cancel pending operation.</param>
		/// <returns></returns>
		public static CallResultTask<T> ToTask<T>(this SteamAPICall_t handle, CancellationToken cancellationToken = default)
			where T : struct
		{
			return new CallResultTask<T>(handle,
				null,
				cancellationToken);
		}

		/// <summary>
		/// Convert call-result handle to a Task like wrapper, to be <see langword="await" />. Continuation will run on .NET thread pool.
		/// <see cref="SynchronizationContext.Current"/> and <see cref="AsyncLocal{T}"/> are not respected by default.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handle">Steam call-result handle will be associated to this awaiting.</param>
		/// <param name="cancellationToken">Cancelation token used to cancel pending operation.</param>
		/// <returns></returns>
		public static CallResultTask<T> GoThreadPool<T>(this SteamAPICall_t handle,
			CancellationToken cancellationToken = default)
			where T : struct
		{
			return new CallResultTask<T>(handle,
				s_scheduleOnThreadPool,
				cancellationToken);
		}

		private readonly static Action<Action> s_scheduleOnThreadPool = (continuation) => ThreadPool.QueueUserWorkItem((s) => s(), continuation, true);

		/// <summary>
		/// Convert call-result to Task like wrapper, to be <see langword="await" />. Continuation will run on current <see cref="SynchronizationContext"/>.
		/// If <see cref="SynchronizationContext.Current"/> is not set, run continuation on .NET thread pool.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handle">Steam call-result handle will be associated to this awaiting.</param>
		/// <param name="cancellationToken">Cancelation token used to cancel pending operation.</param>
		/// <returns></returns>
		public static CallResultTask<T> GoSynchronizationContext<T>(this SteamAPICall_t handle, CancellationToken cancellationToken = default)
			where T : struct
		{
			var syncCtx = SynchronizationContext.Current;

			if (syncCtx == null)
				return GoThreadPool<T>(handle, cancellationToken);

			return new CallResultTask<T>(handle, (action) => syncCtx.Post((s) => ((Action)s!)(), action), cancellationToken)
				.SetCaptureExecutionContext(true);
		}
	}
}

#endif