using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOFittingCosterLib.Models
{
    public class CosterResponse
    {
        public bool AllItemsAppraised { get; set; }
        public string Message { get; set; }
        public decimal Sum { get; set; }
    }
}
