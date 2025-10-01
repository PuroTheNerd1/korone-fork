
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FaggotServer
{
    public class MessageRouterHub : Hub
    {
        // UniverseId -> Topic -> Subscribers
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<string>>> _universes
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<string>>>();

        private string UniverseId
        {
            get
            {
                // In legacy SignalR you use Context.Request directly
                var universe = Context.Request.QueryString["universeId"];
                return string.IsNullOrEmpty(universe) ? "default" : universe;
            }
        }

        private bool IsRCC
        {
            get
            {
                // In legacy SignalR: Context.Request.Headers is NameValueCollection
                var rccAccessKey = Context.Request.Headers["accesskey"];
                return !string.IsNullOrEmpty(rccAccessKey) && rccAccessKey == Configuration.RccAuthorization;
            }
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            if (!IsRCC)
            {
                Console.WriteLine($"[Rejected] {Context.ConnectionId} | Invalid RCC access key");
                // In legacy SignalR you can abort the connection like this:
                Context.Connection.Abort();
                return base.OnDisconnected(true);
            }

            Console.WriteLine($"[Connected] {Context.ConnectionId} | Universe: {UniverseId}");
            return base.OnConnected();
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine($"[Disconnected] {Context.ConnectionId} | Universe: {UniverseId}");

            if (_universes.TryGetValue(UniverseId, out var topics))
            {
                foreach (var topic in topics.Keys.ToList())
                {
                    topics[topic] = new ConcurrentBag<string>(
                        topics[topic].Where(id => id != Context.ConnectionId)
                    );
                }
            }

            return base.OnDisconnected(stopCalled);
        }

        public void Subscribe(string topic, int flags)
        {
            if (!IsRCC) return;

            Console.WriteLine($"[Subscribe] {Context.ConnectionId} -> {topic} (Universe: {UniverseId})");

            var topics = _universes.GetOrAdd(UniverseId,
                _ => new ConcurrentDictionary<string, ConcurrentBag<string>>());

            topics.AddOrUpdate(topic,
                _ => new ConcurrentBag<string> { Context.ConnectionId },
                (_, list) => { list.Add(Context.ConnectionId); return list; });
        }

        public void Publish(string topic, string messageJson, int flags)
        {
            if (!IsRCC) return;

            Console.WriteLine($"[Publish] Universe: {UniverseId} -> {topic} : {messageJson}");

            if (_universes.TryGetValue(UniverseId, out var topics) &&
                topics.TryGetValue(topic, out var subscribers))
            {
                foreach (var connectionId in subscribers)
                {
                    Clients.Client(connectionId).Message(topic, messageJson);
                }
            }
        }
    }
}
