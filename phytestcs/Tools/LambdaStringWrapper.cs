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

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                Console.WriteLine("a");
                try
                {
                    Value = CSharpScript.EvaluateAsync<T>(value, Scene.DefaultScriptOptions.WithReferences(typeof(T).GetTypeInfo().Assembly)).Result;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    Value = null;
                }

                Console.WriteLine("b");
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
            Wrapper = new LambdaStringWrapper<Action<T>>();
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
}