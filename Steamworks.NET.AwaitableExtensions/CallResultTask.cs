// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2025-2025 Akarinnnnn
// Please see the included LICENSE.txt for additional information.

// This file is provided as a sample, copy into your project if you need.


#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if UNITY_2022_3_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
// I(Akarinnnnn) tested that Unity 2022.3 using C# 9, which have NRT
#define STEAMWORKS_SDK_FEATURE_NULLABLE
#nullable enable
#endif

#if !DISABLESTEAMWORKS

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SysDebug = System.Diagnostics.Debug;

namespace Steamworks.NET.AwaitableExtensions
{
	/// <summary>
	/// Low level <see cref="CallResult{T}"/> wrapper of Steamworks.NET awaitable call-result implementation.
	/// </summary>
	/// <typeparam name="T">Type of the result of wrapping call-result</typeparam>
	public class CallResultTask<T> : INotifyCompletion
		where T : struct
	{
		private bool? failed;
		private T result; // `failed` should be checked first before using `result`
		private readonly CallResult<T> completionSource;
		private CancellationToken cancellationToken;
		private CancellationTokenRegistration cancellationRegistration;
		private Action<Action> schedulerDelegate;

		private int getResultInvokedInterlocked = 0;

		private readonly ManualResetEventSlim onCompletedRanEvent;
		private readonly ManualResetEventSlim receivedResultEvent;
		private readonly Action cachedOnCancelled;
		private readonly CallResult<T>.APIDispatchDelegate cachedConvertResult;

#if STEAMWORKS_SDK_FEATURE_NULLABLE
		private Action?
#else
		private Action
#endif
		continuation;

		/// <summary>
		/// Default callback scheduler. This scheduler will run callbacks in the same thread context of <see cref="SteamAPI.RunCallbacks"/>.
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		public static readonly Action<Action> DefaultSchedulerDelegate = (action) => action();

		/// <summary>
		/// Create a task to be <see langword="await"/>ed later. This constructor is internal implementation detail and is subject to change.
		/// </summary>
		/// <remarks>This class is relatively low level to awaitable call-result implementation. Using it after reading xml docs is recommended.</remarks>
		/// <param name="handle">Steam call-result handle which will be associated to this awaiting.</param>
		/// <param name="schedulerDelegate">Completion scheduler delegate. Used to customize user callback invocation location.
		/// Pass <see langword="null"/> will run user callback at same threading context of <see cref="SteamAPI.RunCallbacks"/></param>
		/// <param name="cancellationToken">Cancelation token used to cancel pending operation.</param>
		// Don't Use main constructor, this source file is planned to be used by unity
		public CallResultTask(SteamAPICall_t handle,
#if STEAMWORKS_SDK_FEATURE_NULLABLE
			Action<Action>?
#else
			Action<Action>
#endif
			schedulerDelegate,
			CancellationToken cancellationToken)
		{
			this.schedulerDelegate = schedulerDelegate ?? DefaultSchedulerDelegate;
			this.cancellationToken = cancellationToken;

			receivedResultEvent = new ManualResetEventSlim(false);
			onCompletedRanEvent = new ManualResetEventSlim(false);

			cachedOnCancelled = OnCancelTriggered;
			cachedConvertResult = ConvertResultFromCompletion;

			completionSource = new CallResult<T>();
			completionSource.Set(handle, cachedConvertResult);
			cancellationRegistration = cancellationToken.Register(cachedOnCancelled);
		}

		/// <summary>
		/// Steam API call handle
		/// </summary>
		public SteamAPICall_t Handle => completionSource.Handle;

		/// <summary>
		/// Whether pending Steam API call is completed or cancelled.
		/// </summary>
		public bool IsCompleted => cancellationToken.IsCancellationRequested || failed.HasValue;

		/// <summary>
		/// Reserved for compiler.
		/// </summary>
		/// <remarks>
		/// If <see cref="GetResult"/> is invoked before completed, calling thread will be blocked.
		/// </remarks>
		/// <returns></returns>
		/// <exception cref="SteamCallResultException"></exception>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public T GetResult()
		{
			receivedResultEvent.Wait(cancellationToken);

			cancellationToken.ThrowIfCancellationRequested();

			if (!failed.HasValue)
			{
				SysDebug.Assert(false, "This call-result(handle " + Handle.m_SteamAPICall + ") is not completed.");
				return default;
			}

			if (failed.Value)
			{
				SteamAPICall_t handle = Handle;
				ESteamAPICallFailure failureReason = SteamUtils.GetAPICallFailureReason(handle);
				throw new SteamCallResultException($"Steam API call(result handle {handle}) failed, reason is {failureReason}", failureReason);
			}

			return result;
		}

		/// <summary>
		/// Reserved for compiler.
		/// </summary>
		/// <param name="action"></param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void OnCompleted(Action action)
		{
			continuation = action;

			onCompletedRanEvent.Set();

			if (IsCompleted)
				ScheduleContinuation();
		}

		/// <summary>
		/// Reserved for compiler.
		/// </summary>
		/// <returns></returns>
		public CallResultTask<T> GetAwaiter() => this;

		private void ConvertResultFromCompletion(T result, bool failed)
		{
			this.failed = failed;

			this.result = result;
			receivedResultEvent.Set();

			ScheduleContinuation();
		}

		private void OnCancelTriggered()
		{
			cancellationRegistration.Dispose();
			completionSource.Cancel();

			onCompletedRanEvent.Set();
			receivedResultEvent.Set();

			if (continuation != null)
				ScheduleContinuation();

		}

		private void ScheduleContinuation()
		{
			try
			{
				onCompletedRanEvent.Wait(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				// already cancelled and will be handled by GetResult(). Don't let it leak to outside.
				// just continue dispatching.
			}
			if (Interlocked.CompareExchange(ref getResultInvokedInterlocked, 1, 0) != 0)
			{
				return;
			}

			if (ReferenceEquals(schedulerDelegate, DefaultSchedulerDelegate))
			{
				continuation?.Invoke();
			}
			else
			{
				SysDebug.Assert(continuation != null);
				schedulerDelegate(continuation);
			}
		}

#if false
		/// <summary>
		/// Reset completed <see cref="CallResultTask{T}"/> for next awaiting.
		/// </summary>
		/// <param name="handle">New call-result handle</param>
		/// <param name="schedulerDelegate">Optional. Control where to run continuation. Pass <see langword="null"/> to use previous scheduler delegate.</param>
		/// <param name="cancellationToken"></param>
		/// <exception cref="InvalidOperationException">Previous steam api call's result is not checked yet.</exception>
		public CallResultTask<T> ResetForNextCall(SteamAPICall_t handle,
#if STEAMWORKS_SDK_FEATURE_NULLABLE
			Action<Action>?
#else
			Action<Action>
#endif
			schedulerDelegate,
			CancellationToken cancellationToken)
		{
			failed = null;
			result = default;

			this.schedulerDelegate = schedulerDelegate ?? this.schedulerDelegate;
			this.cancellationToken = cancellationToken;

			receivedResultEvent.Reset();
			onCompletedRanEvent.Reset();
			if (Interlocked.CompareExchange(ref getResultInvokedInterlocked, 0, 1) != 1)
				throw new InvalidOperationException($"Previous steam api call({Handle})'s result is not checked yet. CallResultTask can only be reset after completed.");

			// completionSource initialized
			completionSource.Set(handle, cachedConvertResult);
			cancellationRegistration.Dispose();
			cancellationRegistration = cancellationToken.Register(cachedOnCancelled);

			return this;
		}

#endif
	}
}

#endif