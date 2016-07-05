using System.Collections.Generic;
using System.Threading;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IOFuture"/> of <see cref="IOFuture"/>s.
    /// It is useful when you want to get notified when all <see cref="IOFuture"/>s are complete.
    /// </summary>
    public class CompositeIoFuture<TFuture> : DefaultIoFuture
        where TFuture : IOFuture
    {
        private int _unnotified;
        private volatile bool _constructionFinished;

        /// <summary>
        /// </summary>
        public CompositeIoFuture(IEnumerable<TFuture> children)
            : base(null)
        {
            foreach (var f in children)
            {
                f.Complete += OnComplete;
                Interlocked.Increment(ref _unnotified);
            }

            _constructionFinished = true;
            if (_unnotified == 0)
                Value = true;
        }

        private void OnComplete(object sender, IoFutureEventArgs e)
        {
            if (Interlocked.Decrement(ref _unnotified) == 0 && _constructionFinished)
                Value = true;
        }
    }
}
