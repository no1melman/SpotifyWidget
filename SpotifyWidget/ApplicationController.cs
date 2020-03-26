using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace SpotifyWidget
{
    public interface IShutdownController
    {
        void Shutdown();
    }

    public class ShutdownController : IShutdownController
    {
        private readonly IApplicationController applicationController;

        public ShutdownController(
            IApplicationController applicationController)
        {
            this.applicationController = applicationController;
        }
        public void Shutdown()
        {
            this.applicationController.Shutdown();
        }
    }

    public interface IApplicationController
    {
        void RunWork();

        void Shutdown();
    }

    public class ApplicationController : IApplicationController
    {
        private readonly IEnumerable<IWorkload> workers;
        private IEnumerable<Task> backgroundLoads;
        private readonly CancellationTokenSource cancellationSource;

        public ApplicationController(
            IEnumerable<IWorkload> workers)
        {
            this.workers = workers;
            backgroundLoads = new List<Task>();
            this.cancellationSource = new CancellationTokenSource();
        }

        public void RunWork()
        {
            if (!this.workers.Any())
            {
                return;
            }

            backgroundLoads = workers.Select(x => Task.Run(async () =>
            {
                if (x.Delay > 0)
                {
                    while (true)
                    {
                        await x.DoWork();

                        await Task.Delay(x.Delay, this.cancellationSource.Token);
                    }
                }
                else
                {
                    await x.DoWork();
                }
            }, this.cancellationSource.Token));
            backgroundLoads.ForEach(x =>
            {
               
            });
        }

        public void Shutdown()
        {
            try
            {
                this.cancellationSource?.Cancel();

                Task.Delay(100).Wait(102);

                Application.Current.Shutdown(0);
            }
            catch (AggregateException agex)
            {
                // something here threw from aggregate stuff
                Application.Current.Shutdown(1);
            }
            catch (OperationCanceledException ocex)
            {
                // the task delay wait probably threw
                Application.Current.Shutdown(1);
            }
        }
    }

    public interface IWorkload
    {
        int Delay { get; }
        Task DoWork();
    }

    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (var item in list)
            {
                action(item);
            }
        }
    }
}