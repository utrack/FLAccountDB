using System;
using System.Collections.Concurrent;

namespace FLAccountDB
{
    public class OneShotHandlerQueue<TEventArgs> where TEventArgs : EventArgs
    {
        private readonly ConcurrentQueue<EventHandler<TEventArgs>> _queue;
        public OneShotHandlerQueue()
        {
            _queue = new ConcurrentQueue<EventHandler<TEventArgs>>();
        }
        public void Handle(object sender, TEventArgs e)
        {
            EventHandler<TEventArgs> handler;
            if (_queue.TryDequeue(out handler) && (handler != null))
                handler(sender, e);
        }
        public void Add(EventHandler<TEventArgs> handler)
        {
            _queue.Enqueue(handler);
        }
    }
}
