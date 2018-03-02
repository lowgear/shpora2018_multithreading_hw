using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClusterClient.Utils
{
    public static class Miscellaneous
    {
        private static Random random = new Random();
        public static void RandomShuffle<T>(this T[] list)
        {
            for (var i = list.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }
    }
}