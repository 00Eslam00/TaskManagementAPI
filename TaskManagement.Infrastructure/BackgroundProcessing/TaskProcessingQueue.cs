using System.Threading.Channels;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.BackgroundProcessing;

// In-memory queue built on a Channel. Requests write task ids into it and the
// background worker reads them one by one. Registered as a singleton so both sides share it.
public class TaskProcessingQueue : ITaskProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(taskId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
