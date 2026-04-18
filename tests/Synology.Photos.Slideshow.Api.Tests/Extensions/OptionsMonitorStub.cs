using Microsoft.Extensions.Options;

namespace Synology.Photos.Slideshow.Api.Tests.Extensions;

internal sealed class OptionsMonitorStub<T> : IOptionsMonitor<T> where T : class
{
    public OptionsMonitorStub(T currentValue)
    {
        CurrentValue = currentValue;
    }

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string> listener) => NoopDisposable.Instance;

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }
}
