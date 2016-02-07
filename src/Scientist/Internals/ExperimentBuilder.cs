using System;
using System.Threading.Tasks;

namespace GitHub.Internals
{
    internal class Experiment<T> : IExperiment<T>, IExperimentAsync<T>
    {
        string _name;
        Func<Task<T>> _control;
        Func<Task<T>> _candidate;
        Func<T, T, Task<bool>> _comparator;

        public Experiment(string name)
        {
            _name = name;
        }

        public Experiment(string name, Func<T, T, bool> comparator)
        {
            _name = name;
            if (comparator != null)
                _comparator = (controlResult, candidateResult) => Task.FromResult(comparator(controlResult, candidateResult));
        }

        public Experiment(string name, Func<T, T, Task<bool>> comparator)
        {
            _name = name;
            _comparator = comparator;
        }

        public void Use(Func<Task<T>> control) { _control = control; }

        public void Use(Func<T> control) { _control = () => Task.FromResult(control()); }


        public void Try(Func<Task<T>> candidate) { _candidate = candidate; }

        public void Try(Func<T> candidate) { _candidate = () => Task.FromResult(candidate()); }

        internal ExperimentInstance<T> Build()
        {
            return new ExperimentInstance<T>(_name, _control, _candidate, _comparator);
        }
    }
}
