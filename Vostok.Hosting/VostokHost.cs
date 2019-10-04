﻿using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Commons.Helpers.Observable;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace Vostok.Hosting
{
    [PublicAPI]
    public class VostokHost
    {
        public readonly CancellationTokenSource ShutdownTokenSource;
        public volatile VostokApplicationState ApplicationState;

        private readonly VostokHostSettings settings;
        private readonly CachingObservable<VostokApplicationState> onApplicationStateChanged;
        private readonly IVostokApplication application;
        private readonly VostokHostingEnvironment environment;
        private readonly ILog log;

        public VostokHost([NotNull] VostokHostSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            application = settings.Application;

            ShutdownTokenSource = new CancellationTokenSource();
            
            environment = VostokHostingEnvironmentFactory.Create(settings.EnvironmentSetup, ShutdownTokenSource.Token);

            log = environment.Log.ForContext<VostokHost>();

            onApplicationStateChanged = new CachingObservable<VostokApplicationState>();
            ChangeStateTo(VostokApplicationState.NotInitialized);
        }

        public IObservable<VostokApplicationState> OnApplicationStateChanged => onApplicationStateChanged;

        // CR(iloktionov): Protect against misuse (double or concurrent RunAsync calls).
        public async Task<ApplicationRunResult> RunAsync()
        {
            LogApplicationIdentity(environment.ApplicationIdentity);

            var result = await InitializeApplicationAsync().ConfigureAwait(false)
                         ?? await RunApplicationAsync().ConfigureAwait(false);

            environment.Dispose();

            return result;
        }

        private async Task<ApplicationRunResult> InitializeApplicationAsync()
        {
            log.Info("Initializing application.");
            ChangeStateTo(VostokApplicationState.Initializing);

            try
            {
                await application.InitializeAsync(environment);

                log.Info("Application initialization completed successfully.");
                ChangeStateTo(VostokApplicationState.Initialized);

                return null;
            }
            catch (Exception error)
            {
                log.Error(error, "Unhandled exception has occurred while initializing application.");
                ChangeStateTo(VostokApplicationState.Crashed, error);

                return new ApplicationRunResult(ApplicationRunStatus.ApplicationCrashed, error);
            }
        }

        private async Task<ApplicationRunResult> RunApplicationAsync()
        {
            log.Info("Running application.");
            ChangeStateTo(VostokApplicationState.Running);

            try
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var shutdownToken = environment.ShutdownToken;

                using (shutdownToken.Register(o => ((TaskCompletionSource<bool>)o).TrySetCanceled(), tcs))
                {
                    var applicationTask = application.RunAsync(environment);

                    environment.ServiceBeacon.Start();

                    await Task.WhenAny(applicationTask, tcs.Task).ConfigureAwait(false);

                    environment.ServiceBeacon.Stop();

                    if (shutdownToken.IsCancellationRequested)
                    {
                        log.Info("Cancellation requested, waiting for application to complete with timeout = {Timeout}.", settings.ShutdownTimeout);
                        ChangeStateTo(VostokApplicationState.Stopping);

                        if (!await applicationTask.WaitAsync(settings.ShutdownTimeout).ConfigureAwait(false))
                        {
                            throw new OperationCanceledException($"Cancellation requested, but application has not exited within {settings.ShutdownTimeout} timeout.");
                        }

                        log.Info("Application successfully stopped.");
                        ChangeStateTo(VostokApplicationState.Stopped);
                        return new ApplicationRunResult(ApplicationRunStatus.ApplicationStopped);
                    }

                    log.Info("Application exited.");
                    ChangeStateTo(VostokApplicationState.Exited);
                    return new ApplicationRunResult(ApplicationRunStatus.ApplicationExited);
                }
            }
            catch (Exception error)
            {
                log.Error(error, "Unhandled exception has occurred while running application.");
                ChangeStateTo(VostokApplicationState.Crashed, error);
                return new ApplicationRunResult(ApplicationRunStatus.ApplicationCrashed, error);
            }
        }

        private void ChangeStateTo(VostokApplicationState newState, Exception error = null)
        {
            ApplicationState = newState;
            onApplicationStateChanged.Next(newState);
            if (error != null)
                onApplicationStateChanged.Error(error);
        }

        private void LogApplicationIdentity(IVostokApplicationIdentity applicationIdentity)
        {
            // CR(iloktionov): Log identity's subproject (if present)
            log.Info(
                "Application identity: project: '{Project}', environment: '{Environment}', application: '{Application}', instance: '{Instance}'.",
                applicationIdentity.Project,
                applicationIdentity.Environment,
                applicationIdentity.Application,
                applicationIdentity.Instance);
        }
    }
}