using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    public static class Extensions
    {
        public static List<T> Clone<T>(this IList<T> cloningList) where T : ICloneable
        {
            return cloningList.Select(itemToClone => (T)itemToClone.Clone()).ToList();
        }
    }
}
