using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Industrial_Processing_System_API.Models;

namespace Industrial_Processing_System_API.Utils
{
    public class SystemConfig
    {
        public int WorkerCount { get; set; }
        public int MaxQueueSize { get; set; }
        public List<Job> Jobs { get; set; } = new();
    }
}
