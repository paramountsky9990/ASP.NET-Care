
using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using Quartz;
using Quartz.Impl;
using HGP.Web.Models.ScheduledJob;
using Quartz.Impl.Matchers;

namespace HGP.Web.Utilities
{
    public class JobScheduler
    {
        public static async System.Threading.Tasks.Task StartAsync()
        {
            PendingRequestReminderData reminderData = new PendingRequestReminderData();
            reminderData.SitePendingRequests = new List<SitePendingRequests>();

            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = await factory.GetScheduler();
            await scheduler.Start();

            #region WeeklyAssetUploadSummaryJobScheduler

            IJobDetail job1 = JobBuilder.Create<WeeklyAssetUploadSummaryJob>()
                .WithIdentity("Job1", "group1")
                .Build();

            job1.JobDataMap["cContext"] = HttpContext.Current;

            ITrigger trigger1 = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .WithSchedule(CronScheduleBuilder
            .WeeklyOnDayAndHourAndMinute(DayOfWeek.Monday, 9, 00)
            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")) // Every Monday morning at 9:00 AM PST
            ).Build();

            #endregion

            #region RequestReminderJobScheduler

            IJobDetail job2 = JobBuilder.Create<RequestReminderJob>()
                .WithIdentity("Job2", "group2")
                .Build();

            job2.JobDataMap["cContext"] = HttpContext.Current;
            job2.JobDataMap["reminderData"] = reminderData;

            ITrigger trigger2 = TriggerBuilder.Create()
            .WithIdentity("trigger2", "group2")

            .WithDailyTimeIntervalSchedule
                  (s => s.WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(10, 00))
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")) // Every morning 10:00 AM PST
                  ).Build();

            #endregion

            #region WishListMatchedAssetsJobScheduler

            IJobDetail job3 = JobBuilder.Create<WishListMatchedAssetsJob>()
                .WithIdentity("Job3", "group3")
                .Build();

            job3.JobDataMap["cContext"] = HttpContext.Current;

            ITrigger trigger3 = TriggerBuilder.Create()
           .WithIdentity("trigger3", "group3")

            .WithDailyTimeIntervalSchedule
                  (s => s.WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 00))
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")) // Every morning 1:00 AM PST
                  ).Build();

            #endregion

            #region WishListExpirationReminderJob

            IJobDetail job4 = JobBuilder.Create<WishListExpirationReminderJob>()
               .WithIdentity("Job4", "group4")
               .Build();

            job4.JobDataMap["cContext"] = HttpContext.Current;

            ITrigger trigger4 = TriggerBuilder.Create()
           .WithIdentity("trigger4", "group4")

            .WithDailyTimeIntervalSchedule
                  (s => s.WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 00))
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")) // Every morning 1:00 AM PST
                  ).Build();

            #endregion

            #region AssetExpirationReminderJob

            IJobDetail job5 = JobBuilder.Create<AssetExpirationReminderJob>()
               .WithIdentity("Job5", "group5")
               .Build();

            job5.JobDataMap["cContext"] = HttpContext.Current;

            ITrigger trigger5 = TriggerBuilder.Create()
           .WithIdentity("trigger5", "group5")

            .WithDailyTimeIntervalSchedule
                  (s => s.WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 00))
                    .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")) // Every morning 1:00 AM PST
                  ).Build();

            #endregion

            await scheduler.ScheduleJob(job1, trigger1);
            await scheduler.ScheduleJob(job2, trigger2);
            await scheduler.ScheduleJob(job3, trigger3);
            await scheduler.ScheduleJob(job4, trigger4);
            await scheduler.ScheduleJob(job5, trigger5);
        }
    }
}
