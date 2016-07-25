using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOFittingCosterLib.Models
{
    public class Item : IEqualityComparer<Item>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal CostPU { get; set; }
        public decimal Volume { get; set; }

        public bool Equals(Item x, Item y)
        {
            return x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(Item obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
