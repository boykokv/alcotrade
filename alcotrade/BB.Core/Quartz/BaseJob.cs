using Quartz;

namespace BB.Core.Quartz
{
    public class BaseJob: IJob
    {
        virtual public void Execute(IJobExecutionContext context)
        {
        }

        public TContext Get<TContext>(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var instance = (TContext)dataMap[typeof(TContext).Name];

            return instance;
        }
    }
}
