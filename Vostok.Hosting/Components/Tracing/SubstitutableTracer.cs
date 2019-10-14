﻿using Vostok.Tracing;
using Vostok.Tracing.Abstractions;

namespace Vostok.Hosting.Components.Tracing
{
    internal class SubstitutableTracer : ITracer
    {
        private volatile ITracer baseTracer;

        public SubstitutableTracer(ITracer baseTracer)
        {
            this.baseTracer = baseTracer;
        }

        public void SubstituteWith(ITracer newTracer) =>
            baseTracer = newTracer;

        public ISpanBuilder BeginSpan() =>
            baseTracer.BeginSpan();

        public TraceContext CurrentContext
        {
            get => baseTracer.CurrentContext;
            set => baseTracer.CurrentContext = value;
        }
    }
}