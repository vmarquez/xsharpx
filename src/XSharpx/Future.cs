using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace XSharpx
{
    public sealed class Promise<A> : Future<A>
    {
        private readonly Option<Func<A>> _startFunc;

        private readonly Option<Action> _startAction;

        private Int32 _completed = 0;

        private A _a;  //we could get rid of 'completed' and just check if this is null... 

        private ConcurrentQueue<Action<A>> _queue = new ConcurrentQueue<Action<A>>();

        private Promise(Func<A> start)
        {
            _startAction = Option<Action>.Empty;
            _startFunc = Option.Some(start);
        }

        private Promise(Action start)
        {
            _startFunc = Option<Func<A>>.Empty;
            _startAction = Option.Some(start);
        }

        public Promise()
        {
            _startFunc = Option<Func<A>>.Empty;
            _startAction = Option<Action>.Empty;
        }

        public void Start()
        {
            var t = new Task(() =>
            {
                Console.WriteLine("Hello world");
                //this is completed... 
                _startFunc.ForEach(f => Success(f()));
                _startAction.ForEach(f => f());
            });
            t.Start();
        }

        public void ForEach(Action<A> f)
        {
            _queue.Enqueue(a => f(a));
        }

        //we can make this async if we want, but I think it is better synchronous. Returns false if already called 
        public bool Success(A a)
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
            {
                _a = a;
                foreach (var action in _queue)
                {
                    action(a);
                }
                return true;
            }
            return false;
        }

        public Future<C> Bind<B, C>(Func<A, Future<B>> bindf, Func<A, B, C> map)
        {
            var np = new Promise<C>(() =>
            {
                this._startFunc.ForEach(f => this.Success(f()));
            });

            _queue.Enqueue(a =>  //add to the queue first, so there isn't a race. If we add it needlessly, it doesn't matter, never called again.
                {
                    var p = bindf(a);
                    p.ForEach(b => np.Success(map(a, b)));
                });

            if (_completed > 0)
            {
                var p = bindf(_a);
                p.ForEach(b => np.Success(map(_a, b)));
            }
            return np;
        }



        public Future<B> Map<B>(Func<A, B> mapf)
        {
            return Bind(a => Success(mapf(a)), (a, b) => b);
        }

        public Future<A> Future
        {
            get
            {
                return this;
            }
        }

        public static Future<A> Success<A>(A a)
        {
            var p = new Promise<A>();
            p.Success(a);
            return p;
        }

        public static Future<A> Create(Func<A> f)
        {
            return new Promise<A>(f).Future;
        }
    }

    public interface Future<A>
    {
        Future<C> Bind<B, C>(Func<A, Future<B>> bindf, Func<A, B, C> map);

        Future<B> Map<B>(Func<A, B> map);

        void ForEach(Action<A> f);

        void Start();
    }

    public static class FutureExt
    {
        public static Future<C> SelectMany<A, B, C>(this Future<A> fm, Func<A, Future<B>> bind, Func<A, B, C> select)
        {
            return fm.Bind(bind, select);
        }

        public static Future<B> Select<A, B>(this Future<A> fm, Func<A, B> select)
        {
            return fm.Map(select);
        }
    }
}


