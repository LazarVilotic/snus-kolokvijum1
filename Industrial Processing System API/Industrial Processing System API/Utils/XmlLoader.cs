using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Industrial_Processing_System_API.Models;
using System.Xml.Linq;

namespace Industrial_Processing_System_API.Utils
{
    public class XmlLoader
    {
        public static SystemConfig Load(string path)
        {
            var doc = XDocument.Load(path);

            var root = doc.Element("SystemConfig");

            int workerCount = int.Parse(root.Element("WorkerCount").Value);
            int maxQueueSize = int.Parse(root.Element("MaxQueueSize").Value);

            var jobs = new List<Job>();

            foreach (var jobElem in root.Element("Jobs").Elements("Job"))
            {
                var type = Enum.Parse<JobType>(jobElem.Attribute("Type").Value);

                string payload = jobElem.Attribute("Payload").Value;

                int priority = int.Parse(jobElem.Attribute("Priority").Value);

                var job = new Job(
                    Guid.NewGuid(), 
                    type,
                    payload,
                    priority
                );

                jobs.Add(job);
            }

            return new SystemConfig
            {
                WorkerCount = workerCount,
                MaxQueueSize = maxQueueSize,
                Jobs = jobs
            };
        }
    }
}
