using System;
using System.ComponentModel;
using System.Runtime.Remoting.Contexts;
using log4net;
using PostSharp.Laos;

namespace DelftTools.Utils.Aop
{
    ///<summary>
    /// Implements thread-safe calls for Windows.Forms methods.
    ///</summary>
    [Serializable]
    [Synchronization]
    public class InvokeRequiredAttribute : OnMethodInvocationAspect
    {
        public static ISynchronizeInvoke SynchronizeObject { get; set; }

        private static readonly ILog log = LogManager.GetLogger(typeof(InvokeRequiredAttribute));

/*
        public override bool CompileTimeValidate(MethodBase method)
        {
            if (!method.DeclaringType.Implements(typeof (ISynchronizeInvoke)))
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "InvokeRequired attribute used in class {0} that does not implement ISynchronizeInvoke",
                        method.DeclaringType.Name));
            }
            return base.CompileTimeValidate(method);
        }
*/

        public override void OnInvocation(MethodInvocationEventArgs eventArgs)
        {
            var synchronizeObject = SynchronizeObject ?? eventArgs.Instance as ISynchronizeInvoke;

            if (synchronizeObject == null || !synchronizeObject.InvokeRequired)
            {
                eventArgs.Proceed();
            }
            else
            {
                synchronizeObject.Invoke(eventArgs.Delegate, eventArgs.GetArgumentArray());
            }
        }
    }
}
