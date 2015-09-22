// Copyright (c) Microsoft. All rights reserved.

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
        /// Executes an action for each item in a sequence
        /// </summary>
        /// <typeparam name="TItem">Type of item in the sequence</typeparam>
        /// <param name="sequence">Items to iterate over and execute the action against</param>
        /// <param name="action">Action to execute</param>
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
