﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Hosting.Components.Log
{
    internal class Logs : IDisposable
    {
        private readonly List<(string name, ILog log)> userLogs;
        private readonly Func<ILog, ILog> customization;

        private readonly ILog fileLog;
        private readonly ILog consoleLog;
        private readonly ILog herculesLog;

        public Logs(List<(string name, ILog log)> userLogs, ILog fileLog, ILog consoleLog, ILog herculesLog, Func<ILog, ILog> customization)
        {
            this.userLogs = userLogs;
            this.fileLog = fileLog;
            this.consoleLog = consoleLog;
            this.herculesLog = herculesLog;
            this.customization = customization;
        }

        public int Count(bool withoutHercules = false)
            => ToArray(withoutHercules).Length;

        public ILog BuildCompositeLog(bool withoutHercules = false)
            => customization(BuildCompositeLogInner(withoutHercules));

        public void Dispose()
        {
            (fileLog as IDisposable)?.Dispose();
            ConsoleLog.Flush();
        }

        private ILog BuildCompositeLogInner(bool withoutHercules)
        {
            var logs = ToArray(withoutHercules);

            switch (logs.Length)
            {
                case 0:
                    return new SilentLog();
                case 1:
                    return logs.Single();
                default:
                    return new CompositeLog(logs.ToArray());
            }
        }

        private ILog[] ToArray(bool withoutHercules)
        {
            var logs = new List<ILog>();
            
            logs.AddRange(userLogs.Select(tuple => tuple.log));
            logs.Add(fileLog);
            logs.Add(consoleLog);
            
            if (!withoutHercules)
                logs.Add(herculesLog);

            return logs.Where(l => l != null).ToArray();
        }
    }
}