using System;
using System.Reflection;

namespace HotSsl
{
    class Plugin
    {
        public void Run(String f = @"Simple.dll"){
            var DLL = Assembly.LoadFile(f);
            foreach(Type type in DLL.GetExportedTypes())
            {
                var c = Activator.CreateInstance(type);
                type.InvokeMember("Output", BindingFlags.InvokeMethod, null, c, new object[] {@"Hello"});
            }
        }

        public void Run1(String f = @"Simple.dll"){
            var DLL = Assembly.LoadFile(f);
            foreach(Type type in DLL.GetExportedTypes()){
                dynamic c = Activator.CreateInstance(type);
                c.Output(@"Hello");
            }
        }

        private void ExecuteWithReflection(string methodName,object parameterObject = null)
        {
            Assembly assembly = Assembly.LoadFile("Assembly.dll");
            Type typeInstance = assembly.GetType("TestAssembly.Simple");

            if (typeInstance != null){
                MethodInfo methodInfo = typeInstance.GetMethod(methodName);
                ParameterInfo[] parameterInfo = methodInfo.GetParameters();
                object classInstance = Activator.CreateInstance(typeInstance, null);

                if (parameterInfo.Length == 0){
                    // there is no parameter we can call with 'null'
                    var result = methodInfo.Invoke(classInstance, null);
                }else{
                    var result = methodInfo.Invoke(classInstance,new object[] { parameterObject } );
                }
            }
        }
    }
}

//Assembly.dll
namespace TestAssembly{
    public class Simple{

        public void Hello()
        { 
            var name = Console.ReadLine();
            Console.WriteLine("Hello() called");
            Console.WriteLine("Hello" + name + " at " + DateTime.Now);
        }

        public void Run(string parameters)
        { 
            Console.WriteLine("Run() called");
            Console.Write("You typed:"  + parameters);
        }

        public string TestNoParameters()
        {
            Console.WriteLine("TestNoParameters() called");
            return ("TestNoParameters() called");
        }

        public void Execute(object[] parameters)
        { 
            Console.WriteLine("Execute() called");
            Console.WriteLine("Number of parameters received: "  + parameters.Length);

            for(int i=0;i<parameters.Length;i++){
                Console.WriteLine(parameters[i]);
            }
        }
    }
}

// Simple.dll
namespace DLL
{
    using System;

    public class Simple
    {
        public void Output(string s)
        {
            Console.WriteLine(s);
        }
    }
}
