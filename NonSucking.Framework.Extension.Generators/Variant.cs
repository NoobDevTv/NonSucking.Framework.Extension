//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//namespace NonSucking.Framework.Extension.SumTypes
//{
//    public class Variant<T1, T2> : IEquatable<Variant<T1, T2>>
//    {
//        public static bool IsGenerated => true;
//        public Type ValueType => value.GetType();
//        private readonly object value;
//        private readonly int typeId;
//        public Variant(T1 value) => (typeId, this.value) = (0, value);
//        public bool Contains(T1 value) => 0 == typeId && Equals(value, this.value);
//        public Variant(T2 value) => (typeId, this.value) = (1, value);
//        public bool Contains(T2 value) => 1 == typeId && Equals(value, this.value);
//        public void Get(out T1 value)
//        {
//            if (0 != typeId)
//                throw new TypeMismatchException();
//            value = (T1)this.value;
//        }
//        public bool TryGet(out T1 value)
//        {
//            value = default;
//            if (0 != typeId)
//                return false;
//            value = (T1)this.value;
//            return true;
//        }
//        public void Get(out T2 value)
//        {
//            if (1 != typeId)
//                throw new TypeMismatchException();
//            value = (T2)this.value;
//        }
//        public bool TryGet(out T2 value)
//        {
//            value = default;
//            if (1 != typeId)
//                return false;
//            value = (T2)this.value;
//            return true;
//        }
//        public void Map(Action<T1> action)
//        {
//            if (0 != typeId)
//                throw new TypeMismatchException();
//            action((T1)value);
//        }
//        public TReturn Map<TReturn>(Func<T1, TReturn> selector)
//        {
//            if (0 != typeId)
//                throw new TypeMismatchException();
//            return selector((T1)value);
//        }
//        public void Map(Action<T2> action)
//        {
//            if (1 != typeId)
//                throw new TypeMismatchException();
//            action((T2)value);
//        }
//        public TReturn Map<TReturn>(Func<T2, TReturn> selector)
//        {
//            if (1 != typeId)
//                throw new TypeMismatchException();
//            return selector((T2)value);
//        }
//        public void Map(Action<T1> actionT1, Action<T2> actionT2)
//        {
//            switch (typeId)
//            {
//                case 0: actionT1((T1)value); break;
//                case 1: actionT2((T2)value); break;
//                default: throw new TypeMismatchException();
//            }
//        }
//        public TReturn Map<TReturn>(Func<T1, TReturn> selectorT1, Func<T2, TReturn> selectorT2)
//        {
//            switch (typeId)
//            {
//                case 0: return selectorT1((T1)value);
//                case 1: return selectorT2((T2)value);
//                default: throw new TypeMismatchException();
//            }
//        }
//        public bool Is(Type type)
//            => type == ValueType;
//        public override bool Equals(object other) => other is Variant<T1, T2> variant && Equals(variant);
//        public bool Equals(Variant<T1, T2> other)
//        => other != null
//           && typeId == other.typeId
//           && value.Equals(other.value);
//        public override int GetHashCode() => typeId + -1851467485 + (value?.GetHashCode() ?? 1);
//        public override string ToString() => $"Variant<{ValueType.Name}>({value})";
//        public static implicit operator Variant<T1, T2>(T1 obj) => new Variant<T1, T2>(obj);
//        public static implicit operator Variant<T1, T2>(T2 obj) => new Variant<T1, T2>(obj);
//        public static bool operator ==(Variant<T1, T2> left, Variant<T1, T2> right) => left.Equals(right);
//        public static bool operator !=(Variant<T1, T2> left, Variant<T1, T2> right) => !(left == right);
//    }
//    public static partial class IEnumerableExtensions
//    {
//        public static IEnumerable<TResult> Map<TResult, T1, T2>(this IEnumerable<Variant<T1, T2>> enumerable, Func<T1, TResult> selector)
//        {
//            foreach (var item in enumerable)
//            {
//                if (item.TryGet(out T1 value))
//                    yield return selector(value);
//            }
//        }
//        public static IEnumerable<Variant<T1, T2>> Map<T1, T2>(this IEnumerable<Variant<T1, T2>> enumerable, Action<T1> action)
//        {
//            foreach (var item in enumerable)
//            {
//                if (item.TryGet(out T1 value))
//                    action(value);
//                yield return item;
//            }
//        }
//        public static IEnumerable<TResult> Map<TResult, T1, T2>(this IEnumerable<Variant<T1, T2>> enumerable, Func<T2, TResult> selector)
//        {
//            foreach (var item in enumerable)
//            {
//                if (item.TryGet(out T2 value))
//                    yield return selector(value);
//            }
//        }
//        public static IEnumerable<Variant<T1, T2>> Map<T1, T2>(this IEnumerable<Variant<T1, T2>> enumerable, Action<T2> action)
//        {
//            foreach (var item in enumerable)
//            {
//                if (item.TryGet(out T2 value))
//                    action(value);
//                yield return item;
//            }
//        }
//        public static IEnumerable<TResult> Map<TResult, T1, T2>(this IEnumerable<Variant<T1, T2>> enumerable, Func<T1, TResult> selectorT1, Func<T2, TResult> selectorT2)
//        {
//            foreach (var value in enumerable)
//                yield return value.Map(selectorT1, selectorT2);
//        }
//        public static IEnumerable<Variant<T1, T2>> Map<T1, T2>(this IEnumerable<Variant<T1, T2>> enumerable, Action<T1> actionT1, Action<T2> actionT2)
//        {
//            foreach (var value in enumerable)
//            {
//                value.Map(actionT1, actionT2);
//                yield return value;
//            }
//        }
//    }
//    public static partial class IObservableExtensions
//    {
//        public static IObservable<TResult> Map<TResult, T1, T2>(this IObservable<Variant<T1, T2>> observable, Func<T1, TResult> selector)
//        {
//            return observable.Where(t => t.TryGet(out T1 v)).Select(v => v.Map(selector));
//        }
//        public static IObservable<TResult> Map<TResult, T1, T2>(this IObservable<Variant<T1, T2>> observable, Func<T2, TResult> selector)
//        {
//            return observable.Where(t => t.TryGet(out T2 v)).Select(v => v.Map(selector));
//        }
//        public static IObservable<TResult> Map<TResult, T1, T2>(this IObservable<Variant<T1, T2>> observable, Func<T1, TResult> selectorT1, Func<T2, TResult> selectorT2)
//        {
//            return observable.Select(v => v.Map(selectorT1, selectorT2));
//        }
//        public static IObservable<TResult> MapMany<TResult, T1, T2>(this IObservable<Variant<T1, T2>> observable, Func<IObservable<T1>, IObservable<TResult>> selectorT1, Func<IObservable<T2>, IObservable<TResult>> selectorT2)
//        {
//            return observable.Publish(v => Observable.Merge(v.Map((T1 t) => t).Let(selectorT1), v.Map((T2 t) => t).Let(selectorT2)));
//        }
//    }

//}
