using System;
using System.Collections.Generic;

namespace System.Linq
{
	internal static class RuntimeExtensions
	{
        /// <summary>
        /// Call action delegate for each member of col.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="col">The col.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> col, Action<T> action)
        {
            if (col == null) throw new ArgumentNullException("col");
            if (action == null) throw new ArgumentNullException("action");

            foreach (var item in col)
            {
                action(item);
            }
        }
    }
}
