using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Creates a new list from another list, where each element in the list implements IClonable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cloningList"></param>
        /// <returns></returns>
        public static List<T> Clone<T>(this IList<T> cloningList) where T : ICloneable
        {
            return cloningList.Select(itemToClone => (T)itemToClone.Clone()).ToList();
        }
    }
}
