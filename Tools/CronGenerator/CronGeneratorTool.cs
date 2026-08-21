using DevToolbox.Core;

namespace DevToolbox.Tools.CronGenerator
{
    public class CronGeneratorTool : ITool
    {
        public string Category => "Validators";
        public string Name => "Cron Expression Generator (Quartz)";
        public string Description => "Builds and explains a Quartz cron expression.";

        public Control CreateView() => new CronGeneratorControl();
    }
}
