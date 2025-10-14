using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapPartyAssist.Services {
    internal class DataQueueService {

        //coordinates all data sequence-sensitive operations
        private ConcurrentQueue<(Task, DateTime)> DataTaskQueue { get; init; } = new();
        private SemaphoreSlim DataLock { get; init; } = new SemaphoreSlim(1, 1);
        private readonly Plugin _plugin;

        internal DateTime LastTaskTime { get; set; }

        internal DataQueueService(Plugin plugin) {
            _plugin = plugin;
        }

        internal void Dispose() {
            DataTaskQueue.Clear();
        }

        internal Task<T> QueueDataOperation<T>(Func<T> action) {
#if DEBUG
            var caller = new StackFrame(1, true).GetMethod();
            var callerName = caller?.Name ?? "<未知>";
            var callerType = caller?.DeclaringType?.ToString() ?? "<未知类型>";
            _plugin.Log.Verbose($"添加数据任务：来源 {callerName} {callerType}，队列长度：{DataTaskQueue.Count + 1}");
#endif
            Task<T> t = new(action);
            AddToTaskQueue(t);
            return t;
        }

        internal Task QueueDataOperation(Action action) {
#if DEBUG
            var caller = new StackFrame(1, true).GetMethod();
            var callerName = caller?.Name ?? "<未知>";
            var callerType = caller?.DeclaringType?.ToString() ?? "<未知类型>";
            _plugin.Log.Verbose($"添加数据任务：来源 {callerName} {callerType}，队列长度：{DataTaskQueue.Count + 1}");
#endif
            Task t = new(action);
            AddToTaskQueue(t);
            return t;
        }

        private void AddToTaskQueue(Task task) {
            DataTaskQueue.Enqueue((task, DateTime.Now));
            RunNextTask();
            // return task;
        }

        private void RunNextTask() {
            Task.Run(async () => {
                try {
                    await DataLock.WaitAsync();
                    if(DataTaskQueue.TryDequeue(out (Task task, DateTime timestamp) nextTask)) {
                        LastTaskTime = nextTask.timestamp;
                        nextTask.task.Start();
                        await nextTask.task;
                        if(nextTask.task.GetType().IsAssignableTo(typeof(Task<Task>))) {
                            var nestedTask = nextTask.task as Task<Task>;
                            await nestedTask!.Result;
                        }
                    } else {
                        throw new InvalidOperationException("无法从队列中获取任务！");
                    }
                } catch(Exception e) {
                    _plugin.Log.Error(e, "执行数据任务时发生异常。");
                    //_plugin.Log?.Error(e.StackTrace ?? "");
                } finally {
                    DataLock.Release();
                }
            });
        }
    }
}


