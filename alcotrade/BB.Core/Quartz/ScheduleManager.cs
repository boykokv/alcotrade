using System;
using System.Collections.Generic;
using BB.Core.Log;
using Quartz;
using Quartz.Impl;

namespace BB.Core.Quartz
{
    public interface IExecution
    {
        void Execute();
    }

    public interface IExecutionEx
    {
        void Execute(IJobExecutionContext context);
    }

    public class ExecutePerCallJob<TExecution> : IJob
        where TExecution : IExecution, new()
    {
        public void Execute(IJobExecutionContext context)
        {
           Failover.Execute(()=> typeof(ExecutePerCallJob<TExecution>).ExecuteInMonitor(new TExecution().Execute));
        }
    }

    public class ExecuteSingletonJob<TExecution> : IJob
        where TExecution : class, IExecution
    {
        public void Execute(IJobExecutionContext context)
        {
            Failover.Execute(()=>typeof(ExecuteSingletonJob<TExecution>).ExecuteInMonitor(Current<TExecution>.Instance.Execute));
        }
    }

    public class ExecuteSingletonJobEx<TExecution> : IJob
       where TExecution : class, IExecutionEx
    {
        public void Execute(IJobExecutionContext context)
        {
            Failover.Execute(() => typeof(ExecuteSingletonJobEx<TExecution>).ExecuteInMonitor(()=>Current<TExecution>.Instance.Execute(context)));
        }
    }

    public class ScheduleManager
    {
        private readonly IScheduler _scheduler;

        public ScheduleManager()
        {
            _scheduler = StdSchedulerFactory.GetDefaultScheduler();
        }

        public void ScheduleJob<T>(string[] schedules) where T : IJob
        {
            if (schedules.Length == 0)
                throw new ArgumentException("Расписание не задано");

            var job = CreateJob<T>();
            InitSchedules<T>(schedules, job);
        }

        public void ScheduleJob<T>(string[] schedules, IDictionary<string, object> jobDataMap) where T : IJob
        {
            if (schedules.Length == 0)
                throw new ArgumentException("Расписание не задано");
            if (jobDataMap == null)
                throw new ArgumentException("jobDataMap");

            var job = CreateJob<T>();
            job.JobDataMap.PutAll(jobDataMap);
            InitSchedules<T>(schedules, job);
        }

        public void ScheduleExecutionSingletonJob<T>(string[] schedules) where T: class, IExecution
        {
            ScheduleJob<ExecuteSingletonJob<T>>(schedules);
        }

        public void ScheduleExecutionSingletonJobEx<T>(string[] schedules, IDictionary<string, object> jobDataMap) where T : class, IExecutionEx
        {
            ScheduleJob<ExecuteSingletonJobEx<T>>(schedules, jobDataMap);
        }

        public void ScheduleExecutionPerCallJob<T>(string[] schedules) where T : IExecution, new()
        {
            ScheduleJob<ExecutePerCallJob<T>>(schedules);
        }

        public void ScheduleJob<T, TContext>(string[] schedules, TContext context) where T : IJob
        {
            if (schedules.Length == 0)
                throw new ArgumentException("Расписание не задано");

            var job = CreateJob<T>();
            job.JobDataMap.Put(typeof(TContext).Name, context);

            InitSchedules<T>(schedules, job);
        }

        private IJobDetail CreateJob<T>() where T : IJob
        {
            return JobBuilder.Create<T>().StoreDurably().Build();
        }

        private void InitSchedules<T>(string[] schedules, IJobDetail job) where T : IJob
        {
            _scheduler.AddJob(job, false);

            foreach (var schedule in schedules)
            {
                ITrigger trigger = TriggerBuilder.Create()
                    .WithSchedule(CronScheduleBuilder.CronSchedule(schedule))
                    .ForJob(job)
                    .Build();

                _scheduler.ScheduleJob(trigger);
            }
        }

        public void Start()
        {
            _scheduler.Start();
        }

        public void Stop()
        {
            Logger.Info("Выполнение остановлено");
            _scheduler.Shutdown();
        }
    }
}
