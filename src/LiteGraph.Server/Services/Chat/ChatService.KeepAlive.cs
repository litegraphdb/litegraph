namespace LiteGraph.Server.Services.Chat
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Server-sent-event helpers: every SSE stream the chat service opens gets a keepalive
    /// (Chat.SseKeepAliveSeconds), and every event is written through it.
    /// </summary>
    internal partial class ChatService
    {
        #region KeepAlive-Private-Members

        private readonly ConditionalWeakTable<HttpContextBase, SseKeepAlive> _KeepAlives = new ConditionalWeakTable<HttpContextBase, SseKeepAlive>();

        #endregion

        #region KeepAlive-Private-Methods

        private void BeginSse(HttpContextBase ctx)
        {
            ctx.Response.ServerSentEvents = true;
            _KeepAlives.AddOrUpdate(ctx, new SseKeepAlive(ctx, _Settings.Chat.SseKeepAliveSeconds));
        }

        private async Task SendEventAsync(HttpContextBase ctx, ServerSentEvent evt, bool final)
        {
            if (_KeepAlives.TryGetValue(ctx, out SseKeepAlive keepAlive))
            {
                await keepAlive.SendAsync(evt, final).ConfigureAwait(false);
                if (final)
                {
                    _KeepAlives.Remove(ctx);
                    await keepAlive.DisposeAsync().ConfigureAwait(false);
                }
                return;
            }

            await ctx.Response.SendEvent(evt, final).ConfigureAwait(false);
        }

        #endregion
    }
}
