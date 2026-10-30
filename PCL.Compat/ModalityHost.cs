using System.Diagnostics;
using Avalonia.Threading;

namespace MeloongCore.Compat;

/// <summary>
/// Avalonia 没有 WPF 的 DispatcherFrame / ComponentDispatcher，这里用嵌套 DispatcherFrame 提供一个受控的消息泵。
/// 只能在 UI 线程调用。
/// </summary>
public static class ModalityHost {

    private static int _depth;
    private static readonly Stack<long> EnteredAt = new();

    public static int Depth => _depth;

    /// <summary>
    /// 在当前（UI）线程上泵消息，直到 <paramref name="completed"/> 返回 true。
    /// </summary>
    public static void PumpUntil(Func<bool> completed, int timeoutMs = 60_000) {
        if (!Dispatcher.UIThread.CheckAccess()) {
            throw new InvalidOperationException("ModalityHost.PumpUntil 必须在 UI 线程调用。");
        }
        if (completed()) return;

        _depth++;
        EnteredAt.Push(Environment.TickCount64);
        var start = Environment.TickCount64;
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(15), DispatcherPriority.Send, (_, _) => {
            if (completed() || TimeoutReached(start, timeoutMs)) frame.Continue = false;
        });
        timer.Start();
        try {
            try {
                // 嵌套帧会运行平台事件循环（OS 输入、渲染、DispatcherTimer 都能继续处理）。
                // 仅靠 RunJobs 只排空已入队的任务，输入与重绘会全部冻结。
                Dispatcher.UIThread.PushFrame(frame);
            }
            catch (PlatformNotSupportedException) {
                // 无受控平台实现（例如单元测试）时退回排空式泵。
                PumpWithRunJobs(completed, start, timeoutMs);
            }
        }
        finally {
            timer.Stop();
            EnteredAt.Pop();
            _depth--;
        }
        if (!completed() && TimeoutReached(start, timeoutMs)) {
            throw new TimeoutException($"模态等待超时（{timeoutMs} ms，泵深度 {_depth}）。");
        }
    }

    private static bool TimeoutReached(long start, int timeoutMs)
        => timeoutMs != int.MaxValue && Environment.TickCount64 - start > timeoutMs;

    private static void PumpWithRunJobs(Func<bool> completed, long start, int timeoutMs) {
        while (!completed()) {
            if (TimeoutReached(start, timeoutMs)) {
                throw new TimeoutException($"模态等待超时（{timeoutMs} ms）。");
            }
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(1);
        }
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// 把异步操作在当前线程上同步等待完成（内部泵消息，避免死锁）。
    /// </summary>
    public static T RunSync<T>(Func<Task<T>> operation, int timeoutMs = 60_000) {
        var task = operation();
        if (!task.IsCompleted) PumpUntil(() => task.IsCompleted, timeoutMs);
        return task.GetAwaiter().GetResult();
    }

    public static void RunSync(Func<Task> operation, int timeoutMs = 60_000) {
        var task = operation();
        if (!task.IsCompleted) PumpUntil(() => task.IsCompleted, timeoutMs);
        task.GetAwaiter().GetResult();
    }

    [Conditional("DEBUG")]
    public static void AssertNotPumping(string what) {
        if (_depth > 0) Debug.WriteLine($"[ModalityHost] 在模态泵中调用了 {what}，注意重入。");
    }
}
