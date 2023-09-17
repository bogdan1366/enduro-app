using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnduroApp
{
    public class MyOrderingClass : IComparer<Riders>
    {
        public int Compare(Riders x, Riders y)
        {
            int comp = y.IsFasterThen(x);
            return comp;
        }
    }
}
