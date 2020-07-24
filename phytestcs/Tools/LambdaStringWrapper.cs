using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using phytestcs.Interface;

namespace phytestcs
{
    public class LambdaStringWrapper<T>
        where T : Delegate
    {
        private string _code;
        
        public LambdaStringWrapper(string initial="delegate { }")
        {
            Code = initial;
        }

        public LambdaStringWrapper(T initial)
        {
            Value = initial;    
            _code = "delegate { }";
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value;

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

        public T Value { get; private set; }
    }

    public class EventWrapper<T>
    {
        public event Action<T> Event = delegate { };
        public LambdaStringWrapper<Action<T>> Wrapper { get; }

        public EventWrapper()
        {
            Wrapper = new LambdaStringWrapper<Action<T>>(delegate { });
            Event += x =>
            {
                Wrapper.Value(x);
            };
        }

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