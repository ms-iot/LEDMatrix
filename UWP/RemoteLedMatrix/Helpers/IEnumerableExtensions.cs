
namespace RemoteLedMatrix
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Groups an IEnumerable into sets of a given size
        /// </summary>
        /// <typeparam name="T">Type of object in the enumerable</typeparam>
        /// <param name="source">Objects to group into sets</param>
        /// <param name="max">Maximum size of a given set</param>
        /// <returns>Grouped sets of a specific size</returns>
        public static IEnumerable<List<T>> InSetsOf<T>(this IEnumerable<T> source, int max)
        {
            List<T> setOfT = new List<T>(max);
            foreach (var item in source)
            {
                setOfT.Add(item);
                if (setOfT.Count == max)
                {
                    yield return setOfT;
                    setOfT = new List<T>(max);
                }
            }

            if (setOfT.Any())
            {
                yield return setOfT;
            }
        }

        /// <summary>
        /// Recursively selects elements from a hierarchy
        /// </summary>
        /// <remarks>
        /// Recursive select extension from a StackOverflow reply by Jon Skeet:
        /// http://stackoverflow.com/questions/2012274/how-to-unroll-a-recursive-structure
        /// </remarks>
        /// <typeparam name="T">Type of object that forms the hierarchy</typeparam>
        /// <param name="subjects">Hierarchy to recurse over</param>
        /// <param name="selector">What to select from the hierarchy</param>
        /// <returns>List of elements matching the selector, from any depth in the hierarchy</returns>
        public static IEnumerable<T> SelectRecursive<T>(
            this IEnumerable<T> subjects,
            Func<T, IEnumerable<T>> selector)
        {
            if (subjects == null)
            {
                yield break;
            }

            Queue<T> stillToProcess = new Queue<T>(subjects);

            while (stillToProcess.Count > 0)
            {
                T item = stillToProcess.Dequeue();
                yield return item;
                foreach (T child in selector(item))
                {
                    stillToProcess.Enqueue(child);
                }
            }
        }

        public static void ForEach<TItem>(this IEnumerable<TItem> sequence, Action<TItem> action)
        {
            if (null == sequence)
            {
                return;
            }

            foreach (TItem tItem in sequence)
            {
                action(tItem);
            }
        }
    }
}
