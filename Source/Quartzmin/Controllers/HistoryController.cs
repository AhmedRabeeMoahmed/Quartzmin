using Quartz.Plugins.RecentHistory;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Quartz.Spi;

namespace Quartzmin.Controllers
{
    public class HistoryController : PageControllerBase
     {
        public HistoryController(IExecutionHistoryStore HistStore, ISchedulerPlugin executionHistoryPlugin) :
base(HistStore, executionHistoryPlugin)
        {
        }

        [HttpGet]
        public override async Task<IActionResult> Index()
        {
            ViewBag.HistoryEnabled = histStore != null;

            if (histStore == null)
                return View(null);

            IEnumerable<ExecutionHistoryEntry> history = await histStore.FilterLast(100);

            var list = new List<object>();

            foreach (var h in history.OrderByDescending(x => x.ActualFireTimeUtc))
            {
                string state = "Finished", icon = "check";
                var endTime = h.FinishedTimeUtc;

                if (h.Vetoed)
                {
                    state = "Vetoed";
                    icon = "ban";
                }
                else if (!string.IsNullOrEmpty(h.ExceptionMessage))
                {
                    state = "Failed";
                    icon = "close";
                }
                else if (h.FinishedTimeUtc == null)
                {
                    state = "Running";
                    icon = "play";
                    endTime = DateTime.UtcNow;
                }

                var jobKey = h.Job.Split('.');
                var triggerKey = h.Trigger.Split('.');

                list.Add(new
                {
                    Entity = h,

                    JobGroup = jobKey[0],
                    JobName = jobKey[1],
                    TriggerGroup = triggerKey[0],
                    TriggerName = triggerKey[1],

                    ScheduledFireTimeUtc = h.ScheduledFireTimeUtc?.ToDefaultFormat(),
                    ActualFireTimeUtc = h.ActualFireTimeUtc.ToDefaultFormat(),
                    FinishedTimeUtc = h.FinishedTimeUtc?.ToDefaultFormat(),
                    Duration = (endTime - h.ActualFireTimeUtc)?.ToString("hh\\:mm\\:ss"),
                    State = state,
                    StateIcon = icon,
                });
            }

            return View(list);
        }

    }
}
