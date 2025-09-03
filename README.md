# Steamworks.NET.AwaitableExtensions

Await your call-results by adding this package~

## Examples

### Import this extension first:
```csharp
// Import this extension
using Steamworks.NET.AwaitableExtensions;
```

### Overview


```csharp
private async Task CollectModDetailAndDownload(PublishedFileId_t fileId, CancellationToken ct) {
    var queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new PublishedFileId_t[] {fileId}, 1);

    var result = await SteamUGC.SendQueryUGCRequest(queryHandle)
        .ToTask<SteamUGCQueryCompleted_t>(ct);

    /* use `result` from await above */
    if (!SteamUGC.GetQueryUGCDetails(result.m_handle, 0, out SteamUGCDetails_t details))
        throw new Exception();
    
    this.ModManager.CacheDetails(details);

    SteamUGC.DownloadItem(details.m_nPublishedFileId, false);

    PublishedFileId_t[] childrenIds = new PublishedFileId_t[details.m_unNumChildren];
    if (!SteamUGC.GetQueryUGCChildren(result.m_handle, 0, childrenIds, childrenIds.Length))
        throw new Exception();

    /* subscribe dependencies recursively, demonstrating further more awaitings */
    foreach (var childrenId in childrenIds) {
        await CollectModDetailAndDownload(childrenId);
    }
}
```

### Control where to run resume-delegate

You can also control where to run your code which below await(these code is called continuation in terms of async-await).
`ToTask()` demonstrated above will run continuation in the same threading context of `SteamAPI.RunCallbacks()`.
In this library, it bundles an another similar extension method `GoThreadPool()`. Tasks from this method will run
continuations in .NET thread pool. It's designed for running logic on thread pool.
If you are building a `SynchronizationContext` dependent application(WPF or WinForms for example), `GoSynchronizationContext()`
will be helpful.

If you want to run resume-delegate in other location, you can write a scheduler and pass it to the constructor
of `CallResultTask<T>`, `schedulerDelegate` is the parameter. Passing `null` is equivalent to pass
`CallResultTask<T>.DefaultSchedulerDelegate`, which schedule resume delegate inline. This parameter has some differences
to `ResetForNextCall()`'s one.

### Pooling `CallResultTask<T>`

Inside the `CallResultTask<T>` it have some expensive synchronization object.
To avoid creating them every time, it have a method `ResetForNextCall()` designed for pooling.
It's recommended that encapsule a method for getting task from pool.

```csharp
public CallResultTask<T> ToPooledTask<T>(this SteamAPICall_t handle, CancellationToken cancellationToken) 
    where T : struct
{
    // 1. Get task from pool. Assume you created a task pool called `CallResultTaskPool`.
    var task = CallResultTaskPool.GetTask<SteamUGCQueryCompleted_t>();

    // 2. Reset task for current use
    return task.ResetForNextCall(handle, null, cancellationToken);
    // 2.1 You can also specify a scheduler delegate. Assume your delagate is named `s_schedulerDelegade`
    // return task.ResetForNextCall(handle, s_schedulerDelegate, cancellationToken);
}

// Use

var pooledTask = handle.ToPooledTask<SteamUGCQueryCompleted_t>(ct);
var result = await pooledTask;
CallResultTaskPool.Return<SteamUGCQueryCompleted_t>(pooledTask);
// ...
```

Parameter `schedulerDelegate` of `ResetForNextCall` is different from constructor one. If you pass `null`, scheduler delegate from
last call is used instead.
