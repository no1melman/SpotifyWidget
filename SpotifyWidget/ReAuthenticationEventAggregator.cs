using System;
using System.Threading.Tasks;
using Caliburn.Micro;
using Serilog;

namespace SpotifyWidget
{
    public interface IReAuthenticationEventAggregator
    {
        void SendAuthenticationEvent();
    }

    public class ReAuthenticationEventAggregator : IReAuthenticationEventAggregator
    {
        private readonly ILogger logger;
        private static int AttemptThreshold = 10;

        private readonly Func<object, Task> publishOnBackgroundThreadAsync;
        private int attempts = 0;

        public ReAuthenticationEventAggregator(
            IEventAggregator eventAggregator,
            ILogger logger)
        {
            this.logger = logger;
            publishOnBackgroundThreadAsync = eventAggregator.PublishOnCurrentThreadAsync;
            attempts = 0;
        }

        public void SendAuthenticationEvent()
        {
            OnlyExecuteIfIsntRunning(Task.Run(async () =>
            {
                await Task.Delay(10000);

                attempts++;

                if (attempts > AttemptThreshold)
                {
                    // just stop attempting
                    this.logger.Error("Reached Attempt Threshold ({Threshold}):: SendAuthenticationEvent",
                        AttemptThreshold);
                    return;
                }


                this.logger.Warning("Re attempting Authentication :: SendAuthenticationEvent");
                await publishOnBackgroundThreadAsync(new ReAuthenticateMessage());
            }));
        }

        private Task currentSendAuthEvent = null;
        private void OnlyExecuteIfIsntRunning(Task sendAuthEvent)
        {
            if (currentSendAuthEvent == null)
            {
                currentSendAuthEvent = sendAuthEvent;
                return;
            }

            if (!currentSendAuthEvent.IsCompleted) return;
            
            currentSendAuthEvent = sendAuthEvent;
        }
    }

    public class ReAuthenticateMessage
    {
    }
}
