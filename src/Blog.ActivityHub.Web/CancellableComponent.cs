using Microsoft.AspNetCore.Components;

namespace Blog.ActivityHub.Web;

public class CancellableComponent : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    internal CancellationToken CancellationToken => _cts.Token;

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}