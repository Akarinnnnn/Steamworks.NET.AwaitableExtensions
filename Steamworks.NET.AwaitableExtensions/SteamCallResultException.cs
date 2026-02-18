// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2025-2025 Akainnnnn
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
using System.Runtime.Serialization;

namespace Steamworks.NET.AwaitableExtensions
{
	/// <summary>
	/// Errors happened during Steam call-result. 
	/// </summary>
	[Serializable]
	public class SteamCallResultException : Exception
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="failureReason"></param>

		public SteamCallResultException(string message, ESteamAPICallFailure failureReason) : base(message)
		{
			FailureReason = failureReason;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="failureReason"></param>
		/// <param name="innerException"></param>
		public SteamCallResultException(string message, ESteamAPICallFailure failureReason, Exception innerException) : base(message, innerException)
		{
			FailureReason = failureReason;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="failure"></param>
		/// <param name="handle"></param>
		/// <param name="innerException"></param>
		public SteamCallResultException(string message, ESteamAPICallFailure failure, SteamAPICall_t handle, Exception? innerException = default): base(message, innerException)
		{
			FailureReason = failure;
			SteamAPICallHandle = handle;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		[Obsolete("Unused since .NET Core, one of runtime running our standalone builds")]
		protected SteamCallResultException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		/// <summary>
		/// Detailed failure reason.
		/// </summary>
		public ESteamAPICallFailure FailureReason { get; }
		
		/// <summary>
		/// The Steam API call handle that failed.
		/// </summary>
		public SteamAPICall_t SteamAPICallHandle { get; }
	}
}

#endif