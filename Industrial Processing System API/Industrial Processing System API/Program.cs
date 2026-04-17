using Industrial_Processing_System_API.Models;
using Industrial_Processing_System_API.System;
using Industrial_Processing_System_API.Utils;
static Job GenerateRandomJob(Random rand)
{
    var type = rand.Next(2) == 0 ? JobType.Prime : JobType.IO;

    string payload;

    if (type == JobType.Prime)
    {
        int number = rand.Next(5000, 20000);
        int threads = rand.Next(1, 9);

        payload = $"numbers:{number},threads:{threads}";
    }
    else
    {
        int delay = rand.Next(500, 5000);
        payload = $"delay:{delay}";
    }

    int priority = rand.Next(1, 5);

    return new Job(
        Guid.NewGuid(),
        type,
        payload,
        priority
    );
}
var config = XmlLoader.Load("SystemConfig.xml");

var system = new ProcessingSystem(
    config.MaxQueueSize,
    config.WorkerCount
);

var logger = new Logger("log.txt");

// eventi
system.JobCompleted += async (job, result) =>
{
    string log = $"[{DateTime.UtcNow}] [COMPLETED] {job.Id}, {result}";
    await logger.LogAsync(log);
};

system.JobFailed += async (job, status) =>
{
    string log = $"[{DateTime.UtcNow}] [FAILED] {job.Id}, {status}";
    await logger.LogAsync(log);
};

// ubaci pocetne jobove
foreach (var job in config.Jobs)
{
    system.Submit(job);
}

Console.WriteLine("System started...");
int producerCount = config.WorkerCount;
for (int i = 0; i < producerCount; i++)
{
    Task.Run(async () =>
    {
        var rand = new Random();
        while (true)
        {
            try
            {
                var job = GenerateRandomJob(rand);
                var handle = system.Submit(job);
                Console.WriteLine($"[SUBMITTED] {job.Type} job {job.Id} priority {job.Priority}");
            }
            catch (Exception)
            {
                Console.WriteLine("[QUEUE FULL] Job odbijen");
            }
            await Task.Delay(rand.Next(500, 2000));
        }
    });
}
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromSeconds(15));

        var topJobs = system.GetTopJobs(10);

        Console.WriteLine("\n--- TOP 10 JOBS ---");

        foreach (var job in topJobs)
        {
            Console.WriteLine(
                $"ID: {job.Id} | Type: {job.Type} | Priority: {job.Priority}"
            );
        }

        Console.WriteLine("-------------------\n");
    }
});
Console.ReadLine();