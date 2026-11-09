using System.Threading;

namespace FloatingToolbar
{
    internal static class Program
    {
        private const string MutexName = @"Local\FloatingToolbar.SingleInstance";
        private const string ShowEventName = @"Local\FloatingToolbar.Show";

        [STAThread]
        private static void Main()
        {
            using var mutex = new Mutex(true, MutexName, out bool isFirstInstance);
            if (!isFirstInstance)
            {
                SignalRunningInstance();
                return;
            }

            ApplicationConfiguration.Initialize();

            using var showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            using var form = new FloatingToolbarForm();

            RegisteredWaitHandle? registration = ThreadPool.RegisterWaitForSingleObject(
                showSignal,
                (_, _) => BringFormToFront(form),
                null,
                Timeout.Infinite,
                executeOnlyOnce: false);

            try
            {
                Application.Run(form);
            }
            finally
            {
                registration.Unregister(null);
            }
        }

        private static void BringFormToFront(FloatingToolbarForm form)
        {
            try
            {
                form.BeginInvoke(new Action(form.ShowToolbar));
            }
            catch (Exception)
            {
                // Handle not created yet, or the instance is shutting down.
            }
        }

        private static void SignalRunningInstance()
        {
            try
            {
                if (EventWaitHandle.TryOpenExisting(ShowEventName, out var handle))
                {
                    using (handle)
                    {
                        handle.Set();
                    }
                }
            }
            catch (Exception)
            {
                // Nothing useful to do if the other instance cannot be reached.
            }
        }
    }
}
