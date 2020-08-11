using System;
using Microsoft.CodeAnalysis.Scripting;

namespace phytestcs.Internal
{
    public sealed class LambdaStringWrapper<T> : IRepr
        where T : Delegate
    {
        private string _code = null!;
        private readonly object? _globals;

        public LambdaStringWrapper(string initial = "delegate { }", object? globals = null)
        {
            _globals = globals;
            Code = initial;
        }

        public LambdaStringWrapper(T initial, object? globals = null)
        {
            _globals = globals;
            Value = initial;
            _code = "delegate { }";
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
                
                var ok = false;

                if (value[0] == '{' && value[^1] == '}')
                {
                    try
                    {
                        Value = $"delegate{{return ({value[1..^1]});}}"
                            .Eval<T>(so => ScriptOptions(so.AddReferences(typeof(T).Assembly)), _globals).Result;
                        ok = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    value = "delegate " + value;
                }

                if (!ok)
                    try
                    {
                        Value = value.Eval<T>(so => so.AddReferences(typeof(T).Assembly)).Result;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }
        }

        public T Value { get; private set; } = null!;
        public Func<ScriptOptions, ScriptOptions> ScriptOptions { get; set; } = so => so;

        public string Repr()
        {
            return $"`{Code}`";
        }
    }

    public sealed class EventWrapper<T> : IRepr
    {
        public EventWrapper(object? globals = null)
        {
            Wrapper = new LambdaStringWrapper<Action<T>>(delegate { }, globals);
            Event += x => { Wrapper.Value(x); };
        }

        public LambdaStringWrapper<Action<T>> Wrapper { get; }

        public string Repr()
        {
            return Wrapper.Repr();
        }

        public event Action<T> Event = delegate { };

        public void Invoke(T obj)
        {
            Event(obj);
        }
    }

    public interface IRepr
    {
        public string Repr();
    }
}