using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBRE.Common {
    public static class TaskPool {
        const int MaxTasks = 5000;

        private struct TaskAction {
            public string Name;
            public Task Task;
            public Action<Task, object> OnCompletion;
            public object UserData;
        }

        private static List<TaskAction> taskActions = new List<TaskAction>();

        private static void AddInternal(string name, Task task, Action<Task, object> onCompletion, object userdata) {
            lock (taskActions) {
                if (taskActions.Count >= MaxTasks) {
                    throw new Exception(
                        "Too many tasks in the TaskPool:\n" + string.Join('\n', taskActions.Select(ta => ta.Name))
                    );
                }
                taskActions.Add(new TaskAction() { Name = name, Task = task, OnCompletion = onCompletion, UserData = userdata });
            }
        }

        public static void Add(string name, Task task, Action<Task> onCompletion) {
            AddInternal(name, task, (Task t, object obj) => { onCompletion?.Invoke(t); }, null);
        }

        public static void Add<U>(string name, Task task, U userdata, Action<Task, U> onCompletion) where U : class {
            AddInternal(name, task, (Task t, object obj) => { onCompletion?.Invoke(t, (U)obj); }, userdata);
        }

        public static void Update() {
            lock (taskActions) {
                for (int i = 0; i < taskActions.Count; i++) {
                    if (taskActions[i].Task.IsCompleted) {
                        taskActions[i].OnCompletion?.Invoke(taskActions[i].Task, taskActions[i].UserData);
                        taskActions.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}
