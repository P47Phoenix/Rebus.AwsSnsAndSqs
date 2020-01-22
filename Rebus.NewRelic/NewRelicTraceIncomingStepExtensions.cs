namespace Rebus.NewRelicApi
{
    using Config;
    using Pipeline;
    using Pipeline.Receive;

    public static class NewRelicTraceIncomingStepExtensions
    {
        public static void AddNewRelicIncomingStep(this OptionsConfigurer optionsConfiguration)
        {
            optionsConfiguration.Decorate<IPipeline>(context =>
            {
                var onWorkflowItemCompletedStep = new NewRelicTraceIncomingStep();
                var pipeline = context.Get<IPipeline>();
                return new PipelineStepInjector(pipeline)
                    .OnReceive(onWorkflowItemCompletedStep, PipelineRelativePosition.Before,
                        typeof(DispatchIncomingMessageStep));
            });
        }
    }
}
