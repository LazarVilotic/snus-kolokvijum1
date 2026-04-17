using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Industrial_Processing_System_API.Models;

namespace Industrial_Processing_System_API.System
{
    public class ProcessingSystem
    {
        private int _reportIndex = 0;
        private readonly int _maxQueueSize;
        private readonly ConcurrentBag<Job> _completedJobs = new();
        private readonly ConcurrentBag<Job> _failedJobs = new();
        public event Func<Job, int, Task> JobCompleted;
        public event Func<Job, string, Task> JobFailed;

        private readonly ConcurrentDictionary<Guid, Job> _allJobs = new();
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _jobResults = new();

        private readonly PriorityQueue<Job, int> _queue = new();
        private readonly object _lock = new();

        private readonly List<Task> _workers = new();
        private readonly CancellationTokenSource _cts = new();

        public ProcessingSystem(int maxQueueSize, int workerCount)
        {
            _maxQueueSize = maxQueueSize;

   
            for (int i = 0; i < workerCount; i++)
            {
                _workers.Add(Task.Run(() => WorkerLoop(_cts.Token)));
            }

            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    GenerateReport();
                }
            });
        }

        public JobHandle Submit(Job job)
        {
            if (_allJobs.TryGetValue(job.Id, out _))
            {
                var existingTask = _jobResults[job.Id].Task;
                return new JobHandle(job.Id, existingTask);
            }

            var tcs = new TaskCompletionSource<int>();

            lock (_lock)
            {
                if (_allJobs.ContainsKey(job.Id))
                    return new JobHandle(job.Id, _jobResults[job.Id].Task);

                if (_queue.Count >= _maxQueueSize)
                    throw new Exception("Queue is full");

                _queue.Enqueue(job, job.Priority);
                _allJobs[job.Id] = job;
                _jobResults[job.Id] = tcs;
            }

            return new JobHandle(job.Id, tcs.Task);
        }

        private async Task WorkerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Job job = null;

                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        job = _queue.Dequeue();
                    }
                }

                if (job == null)
                {
                    await Task.Delay(50);
                    continue;
                }

                await ProcessJob(job);
            }
        }

        private async Task ProcessJob(Job job)
        {
            int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    job.StartedAt = DateTime.UtcNow;

                    var executionTask = ExecuteJob(job);

                    var completedTask = await Task.WhenAny(
                        executionTask,
                        Task.Delay(2000) 
                    );

                    if (completedTask != executionTask)
                    {
                        throw new TimeoutException();
                    }

                    int result = await executionTask;

                    job.CompletedAt = DateTime.UtcNow;
                    Console.WriteLine($"[DONE] {job.Type} job {job.Id} = {result}");
                    _completedJobs.Add(job);

                    _jobResults[job.Id].SetResult(result);

                    if (JobCompleted != null)
                        await JobCompleted.Invoke(job, result);

                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        _jobResults[job.Id].SetException(new Exception("ABORT"));

                        _failedJobs.Add(job);

                        if (JobFailed != null)
                            await JobFailed.Invoke(job, "ABORT");

                        return;
                    }
                    else
                    {
                        if (JobFailed != null)
                            await JobFailed.Invoke(job, $"Retry {attempt}");
                    }
                }
            }
        }

        private async Task<int> ExecuteJob(Job job)
        {
            return job.Type switch
            {
                JobType.Prime => await ExecutePrime(job.Payload),
                JobType.IO => await ExecuteIO(job.Payload),
                _ => throw new Exception("Unknown job type")
            };
        }
        private async Task<int> ExecuteIO(string payload)
        {
            int delay = int.Parse(payload.Split(':')[1].Replace("_", ""));
            await Task.Run(() => Thread.Sleep(delay));
            return new Random().Next(0, 101);
        }
        private async Task<int> ExecutePrime(string payload)
        {
            var parts = payload.Split(',');

            int max = int.Parse(parts[0].Split(':')[1].Replace("_", ""));
            int threadCount = int.Parse(parts[1].Split(':')[1]);

            threadCount = Math.Max(1, Math.Min(8, threadCount));

            int count = 0;
            object lockObj = new();

            await Task.Run(() =>
            {
                Parallel.For(2, max + 1, new ParallelOptions
                {
                    MaxDegreeOfParallelism = threadCount
                },
                i =>
                {
                    if (IsPrime(i))
                    {
                        lock (lockObj)
                        {
                            count++;
                        }
                    }
                });
            });

            return count;
        }
        private bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;

            int limit = (int)Math.Sqrt(n);

            for (int i = 3; i <= limit; i += 2)
            {
                if (n % i == 0)
                    return false;
            }

            return true;
        }
        public void GenerateReport()
        {
            var completed = _completedJobs.ToList();
            var failed = _failedJobs.ToList();

            var stats = completed
                .GroupBy(j => j.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    AvgTime = g.Average(j =>
                        (j.CompletedAt.Value - j.StartedAt.Value).TotalMilliseconds)
                });

            var failedStats = failed
                .GroupBy(j => j.Type)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                });

            var doc = new XDocument(
                new XElement("Report",
                    new XElement("Completed",
                        stats.Select(s =>
                            new XElement("JobType",
                                new XAttribute("Type", s.Type),
                                new XAttribute("Count", s.Count),
                                new XAttribute("AvgTimeMs", s.AvgTime)
                            )
                        )
                    ),
                    new XElement("Failed",
                        failedStats.Select(f =>
                            new XElement("JobType",
                                new XAttribute("Type", f.Type),
                                new XAttribute("Count", f.Count)
                            )
                        )
                    )
                )
            );

            SaveReport(doc);
        }
        private void SaveReport(XDocument doc)
        {
            string root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\.."));
            string folder = Path.Combine(root, "Reports");

            Directory.CreateDirectory(folder);

            string fileName = Path.Combine(folder, $"report_{_reportIndex}.xml");

            doc.Save(fileName);

            _reportIndex = (_reportIndex + 1) % 10;
        }

        public IEnumerable<Job> GetTopJobs(int n)
        {
            lock (_lock)
            {
                var items = new List<(Job job, int priority)>();
                var tempQueue = new PriorityQueue<Job, int>();

                while (_queue.Count > 0)
                {
                    _queue.TryDequeue(out var j, out var p);
                    items.Add((j, p));
                    tempQueue.Enqueue(j, p);
                }
                while (tempQueue.Count > 0)
                {
                    tempQueue.TryDequeue(out var j, out var p);
                    _queue.Enqueue(j, p);
                }

                return items.OrderBy(x => x.priority).Take(n).Select(x => x.job).ToList();
            }
        }

        public Job GetJob(Guid id)
        {
            _allJobs.TryGetValue(id, out var job);
            return job;
        }
    }

}
