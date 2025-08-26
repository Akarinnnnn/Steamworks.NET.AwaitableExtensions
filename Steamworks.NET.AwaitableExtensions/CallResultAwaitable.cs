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
using SysDebug = System.Diagnostics.Debug;

namespace Steamworks.NET.AwaitableExtensions
{
	/// <summary>
	/// Low level <see cref="CallResult{T}"/> wrapper of Steamworks.NET awaitable call-result implementation.
	/// </summary>
	/// <typeparam name="T">Type of the result of wrapping call-result</typeparam>
	public class CallResultAwaitable<T> where T : struct
	{
		private bool? failed;
		private T result; // `failed` should be checked first before using `result`
		private readonly CallResult<T> completionSource;
		private readonly bool lockOnCompletion;

#if STEAMWORKS_SDK_FEATURE_NULLABLE
		private readonly object?
#else
		private readonly object
#endif
		syncLock;


		/// <summary>
		/// Create a task to be <see langword="await"/>ed later.
		/// </summary>
		/// <remarks>This class is relatively low level to awaitable call-result implementation. Using it after reading xml docs is recommended.</remarks>
		/// <param name="handle">Steam call-result handle will be associated to this awaiting.</param>
		/// <param name="wrappedCallResult">
		/// Native call result completion source. Both pooled or newly-created can be used.
		/// Notice: <see cref="CallResult{T}.Set(SteamAPICall_t, CallResult{T}.APIDispatchDelegate)"/> will be called.
		/// </param>
		/// <param name="lockOnCompletion">
		/// Will use <see langword="lock"/> to synchronize completion state across threads.
		/// Useful when you can foresee invocations between <see cref="SteamAPI.RunCallbacks"> and <see cref="GetResult"/> have race condition.
		/// As long as external synchronization measures are applied, you can still pass <see langword="false"/> even if race conditions exist.
		/// </param>
		/// <param name="syncLock">
		/// If <paramref name="lockOnCompletion"/> is <see langword="true"/>, this argument will affect synchronization.
		/// If <see langword="null"/> is passed, lock will be taken on <see cref="this"/> instance.
		/// </param>
		/// <param name="schedulerDelegate">Completion scheduler delegate. Used to customize user callback invocation location.</param>
#pragma warning disable IDE0290 // "Use main constructor"
		public CallResultAwaitable(SteamAPICall_t handle,
			CallResult<T> wrappedCallResult,
			bool lockOnCompletion,
#if STEAMWORKS_SDK_FEATURE_NULLABLE
			object?
#else
			object
#endif
			syncLock,
#if STEAMWORKS_SDK_FEATURE_NULLABLE
			Action<Action, object?>?
#else
			Action<Action, object>
#endif
			schedulerDelegate)
#pragma warning restore IDE0290 // Justification = "this source file will be used by unity"
		{
			this.lockOnCompletion = lockOnCompletion;
			completionSource = wrappedCallResult;

			if (lockOnCompletion)
			{
				this.syncLock = syncLock ?? this; 
			}
			
			wrappedCallResult.Set(handle, ConvertResultFromCompletion);
		}

		public SteamAPICall_t Handle => completionSource.Handle;

		public bool IsCompleted => failed.HasValue;

		public T GetResult()
		{
			if (!failed.HasValue)
				throw new InvalidOperationException("This call-result(handle " + Handle.m_SteamAPICall + ") is not completed.");

			if (failed.Value)
			{
				ESteamAPICallFailure failureReason = SteamUtils.GetAPICallFailureReason(Handle);
				throw new SteamCallResultException(failureReason.ToString(), failureReason);
			}

			if (!lockOnCompletion)
			{
				return result;
			}
			else
			{
				SysDebug.Assert(syncLock != null);
				lock (syncLock)
				{
					return result;
				}
			}
		}

		public void OnCompleted(Action action)
		{
			if (IsCompleted)
				action();
		}

		public CallResultAwaitable<T> GetAwaiter() => this;

		private void ConvertResultFromCompletion(T result, bool failed)
		{
			this.failed = failed;
			if (!lockOnCompletion)
			{
				this.result = result;
			}
			else
			{
				SysDebug.Assert(syncLock != null);
				lock (syncLock)
				{

				}
			}
		}
	}
}

#endif