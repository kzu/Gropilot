using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;

namespace Gropilot;

static class AgentExtensions
{
    extension(AgentRunResponse response)
    {
        public bool IsTyping => response.Has(nameof(Typing));

        /// <summary>Sets an additional property on the response.</summary>
        public AgentRunResponse Set<T>(string key, T value)
        {
            (response.AdditionalProperties ??= [])[key] = value;
            return response;
        }

        /// <summary>Checks if the response has an additional property with the specified key.</summary>
        public bool Has(string key) => response.AdditionalProperties?.ContainsKey(key) == true;
    }

    extension(Task<AgentRunResponse> task)
    {
        public TypingAgentResponse WithTyping(CancellationToken cancellation = default)
            => new(GenerateTyping(task, cancellation), task);

        static async IAsyncEnumerable<AgentRunResponse> GenerateTyping(Task<AgentRunResponse> response, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            if (!response.IsCompleted)
                yield return AgentRunResponse.Typing();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var interval = 26_000;
            var tasks = new List<Task>
            {
                Task.Delay(interval, cts.Token),
                response
            };

            while (await Task.WhenAny([.. tasks]) is var completed && completed != response)
            {
                tasks.Remove(completed);
                yield return AgentRunResponse.Typing();
                tasks.Add(Task.Delay(interval, cts.Token));
            }

            cts.Cancel();
        }
    }

    public record struct TypingAgentResponse(IAsyncEnumerable<AgentRunResponse> Typing, Task<AgentRunResponse> Response);

    extension(AgentRunResponse)
    {
        /// <summary>Create an empty response that is a marker for a typing indicator on WhatsApp.</summary>
        public static AgentRunResponse Typing() => new AgentRunResponse().Set(nameof(Typing), true);
    }
}
