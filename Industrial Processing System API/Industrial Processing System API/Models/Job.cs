using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industrial_Processing_System_API.Models
{
    public class Job
    {
        private Guid _id;
        private JobType _type;
        private string _payload;
        private int _priority;

        private DateTime _createdAt;
        private DateTime? _startedAt;
        private DateTime? _completedAt;

        public Guid Id
        {
            get => _id;
            set => _id = value;
        }

        public JobType Type
        {
            get => _type;
            set => _type = value;
        }

        public string Payload
        {
            get => _payload;
            set => _payload = value;
        }

        public int Priority
        {
            get => _priority;
            set => _priority = value;
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        public DateTime? StartedAt
        {
            get => _startedAt;
            set => _startedAt = value;
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set => _completedAt = value;
        }

        public Job(Guid id, JobType type, string payload, int priority)
        {
            _id = id;
            _type = type;
            _payload = payload;
            _priority = priority;
            _createdAt = DateTime.UtcNow;
        }
    }
}
