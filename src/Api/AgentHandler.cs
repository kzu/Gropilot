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

        var typing = gropilot.RunAsync(text.Text, cancellationToken: cancellation).WithTyping(cancellation);
        await foreach (var t in typing.Typing)
            yield return message.Typing();

        var response = await typing.Response;
        var content = response.Messages.LastOrDefault()?.Contents.OfType<Microsoft.Extensions.AI.TextContent>().LastOrDefault();
        
        if (content != null)
            yield return message.Reply(content.Text);
    }
}
