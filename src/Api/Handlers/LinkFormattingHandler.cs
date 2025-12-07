using System.Runtime.CompilerServices;
using Devlooped.WhatsApp;

namespace Gropilot;

/// <summary>
/// Provides extensions for configuring formatting cleanups.
/// </summary>
static class LinkFormattingHandlerExtensions
{
    // NOTE: DO NOT USE LIKE THIS, IT RUNS TOO LATE AFTER SENDRESPONSES
    //public static WhatsAppHandlerBuilder UseFormatting(this WhatsAppHandlerBuilder builder)
    //    => Throw.IfNull(builder).Use((inner, _) => new FormattingHandler(inner));
}

/// <summary>
/// Formats outgoing messages by converting links in text responses to link references.
/// </summary>
class LinkFormattingHandler(IWhatsAppHandler inner) : DelegatingWhatsAppHandler(inner)
{
    public override async IAsyncEnumerable<Response> HandleAsync(IEnumerable<IMessage> messages, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await foreach (var message in base.HandleAsync(messages, cancellation).WithExecutionFlow())
        {
            if (message is TextResponse response && response.Text is { } text && text.Contains("http"))
            {
                yield return response with { Text = MarkdownConverter.ConvertLinks(text) };
            }
            else
            {
                yield return message;
            }
        }
    }
}
