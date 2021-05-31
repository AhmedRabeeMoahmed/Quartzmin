using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartzmin;
using Quartz.Logging;
using Microsoft.Extensions.Logging;
using Quartz.Plugins.RecentHistory;
using Quartz.Plugins.RecentHistory.Impl;
using Quartz.Impl.Matchers;
using Quartz.Spi;

namespace Quartzmin
{
    public class Startup
    {
        // private IHostApplicationLifetime ApplicationLifetime;
        private ILoggerFactory LoggerFactory;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddDirectoryBrowser();
            services.AddSingleton<IExecutionHistoryStore, InProcExecutionHistoryStore>();
            services.AddScoped<ISchedulerPlugin, ExecutionHistoryPlugin>();
            services.AddControllersWithViews();
            services.AddQuartz();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime lifetime)
        {
            LoggerFactory = loggerFactory;
            Quartz.Logging.LogContext.SetCurrentLogProvider(LoggerFactory);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            //app.UseStaticFiles();
            //app.UseDirectoryBrowser();
            app.UseRouting();
            app.UseAuthorization();

            IScheduler _Scheduler = InitializeScheduler("MySQLPOC", "MySQLPOCInstance").Result;

            app.UseQuartzmin(new QuartzminOptions()
            {
                Scheduler = _Scheduler
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Scheduler}/{action=Index}/{id?}");
            });

            //ApplicationLifetime = lifetime;
        }

        private async Task<IScheduler> InitializeScheduler(string InstanceId, string InstanceName)
        {
            var properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceId"] = InstanceId,
                ["quartz.scheduler.instanceName"] = InstanceName,
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.MySQLDelegate, Quartz",
                ["quartz.serializer.type"] = "json",
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                ["quartz.jobStore.useProperties"] = "true",
                ["quartz.jobStore.dataSource"] = "default",
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                ["quartz.dataSource.default.provider"] = "MySql",
                ["quartz.dataSource.default.connectionString"] = @"Server=localhost;Database=QaurtzDB;Uid=user;Pwd=password",
            };

            ISchedulerFactory schedulerFactory = new StdSchedulerFactory(properties);
            return await schedulerFactory.GetScheduler();
        }
    }
}
