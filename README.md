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

You can also control where to run your code which below await(these code is called continuation in terms of async-await).
`ToTask()` demonstrated above will run continuation in the same threading context of `SteamAPI.RunCallbacks()`.
In this library, it bundles an another similar extension method `GoThreadPool()`. Tasks from this method will run
continuations in .NET thread pool. It's designed for running logic on thread pool.
If you are building a `SynchronizationContext` depent application(WPF or WinForms for example), `GoSynchronizationContext()`
will be helpful.

Next major version I plan to add resetting on `CallResultTask<T>` for object pooling.
