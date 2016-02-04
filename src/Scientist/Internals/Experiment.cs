﻿using System;
using System.Threading.Tasks;

namespace GitHub.Internals
{
    internal class ExperimentInstance<T>
    {
        static Random _random = new Random(DateTimeOffset.UtcNow.Millisecond);

        readonly Func<Task<T>> _control;
        readonly Func<Task<T>> _candidate;
        readonly string _name;

        public ExperimentInstance(string name, Func<T> control, Func<T> candidate)
        {
            _name = name;
            _control = () => Task.FromResult(control());
            _candidate = () => Task.FromResult(candidate());
        }

        public ExperimentInstance(string name, Func<Task<T>> control, Func<Task<T>> candidate)
        {
            _name = name;
            _control = control;
            _candidate = candidate;
        }

        public async Task<T> Run()
        {
            // Randomize ordering...
            var runControlFirst = _random.Next(0, 2) == 0;
            ExperimentResult controlResult;
            ExperimentResult candidateResult;

            if (runControlFirst)
            {
                controlResult = await Run(_control);
                candidateResult = await Run(_candidate);
            }
            else
            {
                candidateResult = await Run(_candidate);
                controlResult = await Run(_control);
            }

            bool success = controlResult == candidateResult;

            // TODO: Get that duration!
            var measurement = new Measurement(_name, success, TimeSpan.Zero, TimeSpan.Zero);

            // Fire and forget.
            Publish(measurement);

            if (controlResult.ThrownException != null) throw controlResult.ThrownException;
            return controlResult.Result;
        }

        static async Task<ExperimentResult> Run(Func<Task<T>> experimentCase)
        {
            try
            {
                // TODO: Time this.
                return new ExperimentResult(await experimentCase(), TimeSpan.Zero);
            }
            catch (Exception e)
            {
                return new ExperimentResult(e, TimeSpan.Zero);
            }
        }

        static void Publish(Measurement measurement)
        {
#pragma warning disable 4014
            Task.Run(async () =>
            {
                await Scientist.MeasurementPublisher.Publish(measurement);
            }).ConfigureAwait(false);
#pragma warning restore 4014
        }

        class ExperimentResult
        {
            public ExperimentResult(T result, TimeSpan duration)
            {
                Result = result;
                Duration = duration;
            }

            public ExperimentResult(Exception exception, TimeSpan duration)
            {
                ThrownException = exception;
                Duration = duration;
            }

            public T Result { get; }

            public Exception ThrownException { get; }

            public TimeSpan Duration { get; }
        }
    }
}