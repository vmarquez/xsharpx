using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XSharpx
{

    public class FreeDict<A, B, C>
    {

        public readonly Either<Func<Dictionary<A, B>, C>, Func<Dictionary<A, B>, FreeDict<A, B, C>>> maybeMore;

        public FreeDict(Func<Dictionary<A, B>, C> f)
        {
            maybeMore = Either<Func<Dictionary<A, B>, C>, Func<Dictionary<A, B>, FreeDict<A, B, C>>>.Left(f);
        }

        public FreeDict(Func<Dictionary<A, B>, FreeDict<A, B, C>> f)
        {
            maybeMore = Either<Func<Dictionary<A, B>, C>, Func<Dictionary<A, B>, FreeDict<A, B, C>>>.Right(f);
        }

        //UNSAFE
        public C Run
        {
            get
            {
                return RunRec(new Dictionary<A, B>());
            }
        }

        private C RunRec(Dictionary<A, B> t)
        {

            return maybeMore.Fold(f => f(t), mf =>
            {
                var fd = mf(t);
                return fd.RunRec(t);
            });
        }

        public static FreeDict<A, B, Unit> Add(A a, B b)
        {
            return new FreeDict<A, B, Unit>(dic =>
            {
                dic.Add(a, b);
                return new Unit();
            });
        }

        public static FreeDict<A, B, B> Get(A a)
        {
            return new FreeDict<A, B, B>(dic => dic[a]);
        }
    }

    public static class FreeDictExt
    {

        public static FreeDict<A, B, D> Select<A, B, C, D>(this FreeDict<A, B, C> fd, Func<C, D> select)
        {
            return fd.maybeMore.Fold(f =>
            {
                Func<Dictionary<A, B>, D> testf = (dic) => select(f(dic)); //type inference fails on constructor?
                return new FreeDict<A, B, D>(testf);
            },
            mf =>
               new FreeDict<A, B, D>((dic) => mf(dic).Select(c => select(c))));
        }


        public static FreeDict<A, B, E> SelectMany<A, B, C, D, E>(this FreeDict<A, B, C> fd, Func<C, FreeDict<A, B, D>> bind, Func<C, D, E> select)
        {
            return fd.SelectMany(c => bind(c).Select(d => select(c, d)));
        }

        public static FreeDict<A, B, D> SelectMany<A, B, C, D>(this FreeDict<A, B, C> fd, Func<C, FreeDict<A, B, D>> bind)
        {
            return fd.maybeMore.Fold(f =>
            new FreeDict<A, B, D>((dic) =>
               bind(f(dic))
                ),
            mf =>
                new FreeDict<A, B, D>((dic) =>
                    mf(dic).SelectMany(c => bind(c)))
            );
        }


    }
}
