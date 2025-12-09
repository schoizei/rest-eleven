using Microsoft.JSInterop;

namespace RestEleven.Client.Services;

public class BackgroundSyncService : IBackgroundSyncService
{
    private readonly IJSRuntime _jsRuntime;

    public BackgroundSyncService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task RegisterAsync(string tag, CancellationToken cancellationToken = default)
    {
        var supported = await IsSupportedAsync(cancellationToken);
        if (!supported)
        {
            return;
        }

        await _jsRuntime.InvokeVoidAsync("resteleven.sync.register", cancellationToken, tag);
    }

    public async Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("resteleven.sync.supported", cancellationToken);
        }
        catch (JSException)
        {
            return false;
        }
    }
}
