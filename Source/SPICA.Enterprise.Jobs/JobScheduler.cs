using System;
using Quartz;
using Quartz.Impl;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace SPICA.Enterprise.Jobs
{
    public class JobScheduler //: IJobScheduler
    {
        public static IScheduler Scheduler;
        private string _instanceName;
        private string _instanceId;
        public JobScheduler(string InstanceId, string InstanceName)
        {
            _instanceId = InstanceId;
            _instanceName = InstanceName;
        }

        public void Start()
        {
            Scheduler.Start();
        }

        public async Task StartAsync()
        {
            await Scheduler.Start();
        }

        public void Stop()
        {
            if (Scheduler == null)
            {
                return;
            }

            // give running jobs 30 sec (for example) to stop gracefully
            if (Scheduler.Shutdown(waitForJobsToComplete: true).Wait(30000))
            {
                Scheduler = null;
            }
            else
            {
                // jobs didn't exit in timely fashion - log a warning...
            }
        }


        public async Task<IJobDetail> GetJob(Type JobType, string GroupName)
        {
            JobKey jobKey = JobKey.Create(JobType.Name, GroupName);
            IJobDetail job = await Scheduler.GetJobDetail(jobKey);
            return job;
        }

        public async Task DeleteJob(Type JobType, string GroupName)
        {
            JobKey jobKey = JobKey.Create(JobType.Name, GroupName);
            await Scheduler.DeleteJob(jobKey);
        }

        public async Task<IJobDetail> CreateJob(Type JobType, string GroupName)
        {
            JobKey jobKey = JobKey.Create(JobType.Name, GroupName);

            IJobDetail job = await Scheduler.GetJobDetail(jobKey);

            if (job == null)
            {
                job = JobBuilder.Create(JobType)
                                 .WithIdentity(JobType.Name, GroupName)
                                 .Build();
            }

            return job;
        }

        public IJobDetail CreateJob(IJob JobObject, string GroupName)
        {
            Type JobType = JobObject.GetType();

            return JobBuilder.Create(JobType)
                             .WithIdentity(JobType.Name, GroupName)
                             .Build();
        }

        public ITrigger CreateTrigger(string TriggerName, string GroupName, TimeSpan timeSpan)
        {
            return TriggerBuilder.Create()
                .WithIdentity(TriggerName, GroupName)
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(timeSpan)
                    .RepeatForever())
                .Build();
        }

        public async Task ScheduleJob(IJobDetail job, ITrigger trigger)
        {
            await Scheduler.ScheduleJob(job, trigger);
        }

        public async Task RescheduleJob(string TriggerName, string GroupName)
        {
            ITrigger trigger = await this.GetTrigger(TriggerName, GroupName);
            TriggerKey triggerKey = new TriggerKey(TriggerName, GroupName);

            await Scheduler.RescheduleJob(triggerKey, trigger); //Reschedule Same trigger
        }

        public async Task<ITrigger> GetTrigger(string TriggerName, string GroupName)
        {
            TriggerKey triggerKey = new TriggerKey(TriggerName, GroupName);

            return await Scheduler.GetTrigger(triggerKey);
        }

        public static async Task<JobScheduler> CreateAsync(string InstanceId, string InstanceName)
        {
            JobScheduler x = new JobScheduler(InstanceId, InstanceName);
            await x.InitializeScheduler();
            return x;
        }

        private async Task InitializeScheduler()
        {
            var properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = _instanceName,
                ["quartz.scheduler.instanceId"] = _instanceId,
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.MySQLDelegate, Quartz",
                ["quartz.serializer.type"] = "json",
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                ["quartz.jobStore.useProperties"] = "true",
                ["quartz.jobStore.dataSource"] = "default",
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                ["quartz.dataSource.default.provider"] = "MySql",
                ["quartz.dataSource.default.connectionString"] = @"Server=localhost;Database=SPICA_Enterprise_Master;Uid=mysqluser;Pwd=spic@pop123"
            };

            ISchedulerFactory schedulerFactory = new StdSchedulerFactory(properties);
            Scheduler = await schedulerFactory.GetScheduler();
        }

    }
}
