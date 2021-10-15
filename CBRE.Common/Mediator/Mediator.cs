using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CBRE.Common.Mediator {
    /// <summary>
    /// The mediator is a static event/communications manager.
    /// </summary>
    public static class Mediator {
        public delegate void MediatorExceptionEventHandler(object sender, MediatorExceptionEventArgs e);
        public static event MediatorExceptionEventHandler MediatorException;
        private static void OnMediatorException(object sender, Enum message, object parameter, Exception ex) {
            if (MediatorException != null) {
                var st = new StackTrace();
                var frames = st.GetFrames() ?? new StackFrame[0];
                var msg = "Mediator exception: " + message + "(" + parameter + ")";
                foreach (var frame in frames) {
                    var method = frame.GetMethod();
                    msg += "\r\n    " + method.ReflectedType.FullName + "." + method.Name;
                }
                MediatorException(sender, new MediatorExceptionEventArgs(message, new Exception(msg, ex)));
            }
        }

        /// <summary>
        /// Helper method to execute the a function with the same name as the message. Called by the listener if desired.
        /// </summary>
        /// <param name="obj">The object to call the method on</param>
        /// <param name="message">The name of the method</param>
        /// <param name="parameter">The parameter. If this is an array, the multi-parameter method will be given priority over the single- and zero-parameter methods</param>
        public static bool ExecuteDefault(object obj, Enum message, object parameter) {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var t = obj.GetType();
            MethodInfo method = null;
            object[] parameters = null;
            if (parameter is object[]) {
                var arr = (object[])parameter;
                method = t.GetMethod(message.ToString(), flags, null, arr.Select(x => x == null ? typeof(object) : x.GetType()).ToArray(), null);
                parameters = arr;
            }
            if (method == null && parameter != null) {
                method = t.GetMethod(message.ToString(), flags, null, new[] { parameter.GetType() }, null);
                parameters = new[] { parameter };
            }
            if (method == null) {
                method = t.GetMethod(message.ToString(), flags);
                if (method != null) parameters = method.GetParameters().Select(x => (object)null).ToArray();
            }
            if (method != null) {
                var sync = obj as ISynchronizeInvoke;
                if (sync != null && sync.InvokeRequired) sync.Invoke(new Action(() => method.Invoke(obj, parameters)), null);
                else method.Invoke(obj, parameters);
                return true;
            }
            return false;
        }

        private struct Listener {
            public WeakReference Ref { get; init; }
            public readonly int Priority { get; init; }
        }
        
        private static readonly MultiDictionary<Enum, Listener> Listeners;

        static Mediator() {
            Listeners = new MultiDictionary<Enum, Listener>();
        }

        public static void Subscribe(Enum message, IMediatorListener obj, int priority = 0) {
            Listeners.AddValue(message, new Listener { Ref = new WeakReference(obj), Priority = priority });
            foreach (var list in Listeners.Values) {
                list.Sort((l1, l2) => l1.Priority-l2.Priority);
            }
        }

        public static void UnsubscribeAll(IMediatorListener obj) {
            foreach (var listener in Listeners.Values) {
                listener.RemoveAll(x => !x.Ref.IsAlive || x.Ref.Target == null || x.Ref.Target == obj);
            }
        }

        public static void Publish(Enum message, params object[] parameters) {
            object parameter = null;
            if (parameters.Length == 1) parameter = parameters[0];
            else if (parameters.Length > 1) parameter = parameters;
            Publish(message, parameter);
        }

        private readonly static Stack<Enum> messageStack = new Stack<Enum>();
        public static void Publish(Enum message, object parameter = null) {
            if (!Listeners.ContainsKey(message)) { return; }
            string debugLine = "";
            foreach (Enum msg in messageStack) {
                debugLine += $"{msg} > ";
            }
            debugLine += message;
            Debug.WriteLine(debugLine);
            messageStack.Push(message);
            var list = Listeners[message].ToArray();
            foreach (var listener in list) {
                Debug.WriteLine($"{listener.Priority} {listener.Ref.Target?.GetType() ?? typeof(int)}");
                var target = listener.Ref.Target;
                if (target is null || !listener.Ref.IsAlive) {
                    Listeners.RemoveValue(message, listener);
                    continue;
                }
                var method = target.GetType().GetMethod("Notify", new[] { typeof(Enum), typeof(object) });
                if (method != null) {
                    try {
                        method.Invoke(target, new[] { message, parameter });
                    } catch (Exception ex) {
                        OnMediatorException(method, message, parameter, ex);
                    }
                }
            }
            messageStack.Pop();
        }
    }
}
