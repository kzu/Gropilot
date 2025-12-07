using Devlooped.WhatsApp;

namespace Gropilot;

/// <summary>
/// Sets up a behavior that short-cirtuits processing of status and unsupported messages,
/// </summary>
static class IgnoreMessagesExtensions
{
    /// <summary>
    /// Sets up a default filter handler that ignores status and unsupported messages.
    /// </summary>
    public static WhatsAppHandlerBuilder UseIgnore(this WhatsAppHandlerBuilder builder)
        => Throw.IfNull(builder).Use((inner, services) => new IgnoreMessagesHandler(inner,
            message => message.Type != MessageType.Status && message.Type != MessageType.Unsupported));

    /// <summary>
    /// Sets up a custom filter handler that ignores messages based on the provided filter function.
    /// </summary>
    public static WhatsAppHandlerBuilder UseIgnore(this WhatsAppHandlerBuilder builder, Func<IMessage, bool> filter)
        => Throw.IfNull(builder).Use((inner, services) => new IgnoreMessagesHandler(inner, filter));

    class IgnoreMessagesHandler(IWhatsAppHandler inner, Func<IMessage, bool> filter) : DelegatingWhatsAppHandler(inner)
    {
        public override IAsyncEnumerable<Response> HandleAsync(IEnumerable<IMessage> messages, CancellationToken cancellation = default)
        {
            var filtered = messages.Where(filter).ToArray();
            // Skip inner handler altogether if no messages pass the filter.
            if (filtered.Length == 0)
                return AsyncEnumerable.Empty<Response>();

            return base.HandleAsync(filtered, cancellation).WithExecutionFlow();
        }
    }
}