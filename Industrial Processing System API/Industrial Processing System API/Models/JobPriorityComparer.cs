using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industrial_Processing_System_API.Models
{
    public class JobPriorityComparer : IComparer<Job>
    {
        public int Compare(Job x, Job y)
        {
            return x.Priority.CompareTo(y.Priority);
        }
    }
}
