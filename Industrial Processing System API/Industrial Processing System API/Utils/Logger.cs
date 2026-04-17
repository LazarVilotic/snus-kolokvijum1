using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industrial_Processing_System_API.Utils
{
    public class Logger
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public Logger(string filePath)
        {
            _filePath = filePath;
        }

        public async Task LogAsync(string message)
        {
            await _semaphore.WaitAsync();

            try
            {
                using var writer = new StreamWriter(_filePath, append: true);
                await writer.WriteLineAsync(message);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
