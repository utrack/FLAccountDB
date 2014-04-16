using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System
{
    public class WTuple<T1, T2>
    {
        public T1 Item1
        { get; set; }

        public T2 Item2
        { get; set; }

        public WTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
        
 
    }
}
