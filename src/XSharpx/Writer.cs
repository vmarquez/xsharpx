using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSharpx
{

    public class WriterTTask<A, B>
    {
        public readonly Task<B> task;
        public readonly A a;
        public readonly Semigroup<A> sg;

        public WriterTTask(A a, Task<Tuple<A,B>> t, Semigroup<A> semigroup)
        {
            task = t;
            this.a = a;
            sg = semigroup;
        }

        private WriterTTask()
        {

        }

    }


    public class IEnumerableTWriter<A, B>
    {
        public readonly IEnumerable <B> ie;
        public readonly A a;
        public readonly Semigroup<A> semigroup;

        public IEnumerableTWriter(A a, IEnumerable<B> ie, Semigroup<A> semigroup)
        {
            this.a = a;
            this.ie = ie;
            this.semigroup = semigroup;
        }

        private IEnumerableTWriter() { }
    }
    
    public class Writer<A, B>
    {
        public readonly A a;
        public readonly B b;
        public readonly Semigroup<A> semigroup;

        public Writer(A a, B b, Semigroup<A> semigroup)
        {
            this.a = a;
            this.b = b;
            this.semigroup = semigroup;
        }

        private Writer() { } 

        public Tuple<A, B> Apply()
        {
            return new Tuple<A, B>(a, b);
        }
    }

    public static class WriterExtensions
    {
        public static Writer<A, C> Select<A, B, C>(this Writer<A, B> w, Func<B, C> f)
        {
            var c = f(w.b);
            return new Writer<A, C>(w.a, c, w.semigroup);
        }

        
        public static Writer<A, D> SelectMany<A, B, C, D>(this Writer<A, B> w, Func<B, Writer<A,C>> bind, Func<B, C, D> select)
        {
            return SelectMany(w, b => Select(bind(b), c => select(b, c)));
        }

        public static Writer<A, C> SelectMany<A, B, C>(this Writer<A, B> w, Func<B, Writer<A, C>> f)
        {
            var wb = f(w.b);
            var na = w.semigroup.Apply(w.a, wb.a);
            return new Writer<A, C>(na, wb.b, wb.semigroup);
        }
    }

    public static class IEnumerableTWriterExtensions
    {
        public static IEnumerableTWriter<A, D> SelectMany<A, B, C, D>(this IEnumerableTWriter<A, B> w, Func<B, IEnumerableTWriter<A,C>> bind, Func<B, C, D> select) 
        {
            return SelectMany(w, b => Select(bind(b), c => select(b, c)));
        }

        public static IEnumerableTWriter<A, C> SelectMany<A, B, C>(this IEnumerableTWriter<A, B> w, Func<B, IEnumerableTWriter<A, C>> f)
        {

            var id = new IEnumerableTWriter<A, C>(default(A), new System.Collections.Generic.List<C>(), w.semigroup);
            var fie = w.ie.Select(b => f(b)).Aggregate(id, ( itw1, itw2) =>
            {
                var nie = itw1.ie.Concat(itw2.ie);
                return new IEnumerableTWriter<A, C>(itw2.a, nie, w.semigroup); //we don't want to accumulate multiple copies of the nested IEnumerableTWriter's A
            });
            var na = fie.semigroup.Apply(w.a, fie.a); //NPE if the semigroups are null, should we handle that?
            return new IEnumerableTWriter<A, C>(na, fie.ie, w.semigroup);
            
        }

        public static IEnumerableTWriter<A, C> Select<A, B, C>(this IEnumerableTWriter<A, B> w, Func<B, C> f)
        {
           var nie =  w.ie.Select(b => f(b));
           return new IEnumerableTWriter<A, C>(w.a, nie, w.semigroup);
        }
        
        public static IEnumerableTWriter<A,B>  Where<A,B>(this IEnumerableTWriter<A, B> w, Func<B, Boolean> f)
        {
            var nie = w.ie.Where(b => f(b));
            return new IEnumerableTWriter<A, B>(w.a, nie, w.semigroup);
        }
        
    }
}
