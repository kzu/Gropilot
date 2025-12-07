using System.Runtime.CompilerServices;
using Devlooped.WhatsApp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Gropilot;

class AgentHandler([FromKeyedServices("gropilot")] AIAgent gropilot) : IWhatsAppHandler
{
    public async IAsyncEnumerable<Response> HandleAsync(IEnumerable<IMessage> messages, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        // For now, we support only content messages (from user)
        var message = messages.OfType<ContentMessage>().LastOrDefault();
        if (message is null)
            yield break;

        // For now, we support only text messages (user can send other types too)
        if (message.Content is not TextContent text)
            yield break;

        var response = await gropilot.RunAsync(text.Text, cancellationToken: cancellation);

        yield return message.Reply(response.Text);
    }
}
