using System.Threading;
using System;
using System.Threading.Tasks;
using Quartz;

namespace SPICA.Enterprise.Jobs
{
    public class HelloJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Console.Out.WriteLineAsync("HelloJob Started!");
            // Thread.Sleep(15000);
            // await Console.Out.WriteLineAsync("HelloJob Finished!");
        }
    }
}