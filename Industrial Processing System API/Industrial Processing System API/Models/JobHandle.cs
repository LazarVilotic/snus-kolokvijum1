using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industrial_Processing_System_API.Models
{
    public class JobHandle
    {
        private Guid _id;
        private Task<int> _result;

        public Guid Id
        {
            get => _id;
            set => _id = value;
        }

        public Task<int> Result
        {
            get => _result;
            set => _result = value;
        }

        public JobHandle(Guid id, Task<int> result)
        {
            _id = id;
            _result = result;
        }
    }
}
