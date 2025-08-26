// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2025 Riley Labrecque
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
	[Serializable]
	public class SteamCallResultException : Exception
	{

		public SteamCallResultException(string message, ESteamAPICallFailure failureReason) : base(message)
		{
			FailureReason = failureReason;
		}

		public SteamCallResultException(string message, ESteamAPICallFailure failureReason, Exception innerException) : base(message, innerException)
		{
			FailureReason = failureReason;
		}

		[Obsolete("Unused since .NET Core, one of runtime running our standalone builds")]
		protected SteamCallResultException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public ESteamAPICallFailure FailureReason { get; private set; }
	}
}

#endif