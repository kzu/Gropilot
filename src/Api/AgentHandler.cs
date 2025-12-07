using System.Runtime.CompilerServices;
using Devlooped.WhatsApp;

namespace Gropilot;

class AgentHandler : IWhatsAppHandler
{
    public async IAsyncEnumerable<Response> HandleAsync(IEnumerable<IMessage> messages, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var message = messages.OfType<UserMessage>().LastOrDefault();
        if (message is null)
            yield break;

        yield return message.Reply("Hello!");
    }
}
