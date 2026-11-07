// SPDX-License-Identifier: Apache-2.0
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MeloongCore;
using Xunit;

namespace PCL.Tests;

/// <summary>
/// ModNet 网络层与本地 HTTP 服务器的集成测试。
/// </summary>
public class Step10NetTests {

    private const int PumpTimeoutMs = 30000;

    private sealed class LocalServer : IDisposable {
        private readonly HttpListener _listener = new();
        private readonly Thread _thread;
        private readonly byte[] _payload;
        private int _retryCount;
        private volatile bool _disposed;

        public int Port { get; }
        public string BaseUrl => $"http://127.0.0.1:{Port}";
        public int RequestCount => Volatile.Read(ref _requestCount);
        private int _requestCount;

        public LocalServer(byte[] payload) {
            _payload = payload;
            Port = GetFreePort();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _thread = new Thread(Loop) { IsBackground = true, Name = "LocalServer" };
            _thread.Start();
        }

        private static int GetFreePort() {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private void Loop() {
            while (!_disposed && _listener.IsListening) {
                HttpListenerContext context;
                try {
                    context = _listener.GetContext();
                } catch {
                    return;
                }
                Interlocked.Increment(ref _requestCount);
                ThreadPool.QueueUserWorkItem(_ => Handle(context));
            }
        }

        private void Handle(HttpListenerContext context) {
            try {
                var path = context.Request.Url?.AbsolutePath ?? "/";
                switch (path) {
                    case "/ok":
                        Write(context, 200, Encoding.UTF8.GetBytes("hello-pcl"), "text/plain");
                        break;
                    case "/missing":
                        Write(context, 404, Encoding.UTF8.GetBytes("not-found"), "text/plain");
                        break;
                    case "/retry":
                        if (Interlocked.Increment(ref _retryCount) == 1) {
                            Write(context, 503, Encoding.UTF8.GetBytes("busy"), "text/plain");
                        } else {
                            Write(context, 200, Encoding.UTF8.GetBytes("retry-ok"), "text/plain");
                        }
                        break;
                    case "/file":
                        Write(context, 200, _payload, "application/octet-stream");
                        break;
                    case "/slow":
                        WriteSlow(context);
                        break;
                    default:
                        Write(context, 404, Array.Empty<byte>(), "text/plain");
                        break;
                }
            } catch {
                // 服务器关闭或客户端中断时忽略
            } finally {
                try {
                    context.Response.Close();
                } catch {
                }
            }
        }

        private static void Write(HttpListenerContext context, int status, byte[] body, string contentType) {
            context.Response.StatusCode = status;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = body.Length;
            if (body.Length > 0) context.Response.OutputStream.Write(body, 0, body.Length);
        }

        private void WriteSlow(HttpListenerContext context) {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength64 = _payload.Length;
            for (var offset = 0; offset < _payload.Length; offset += 4096) {
                if (_disposed) return;
                var count = Math.Min(4096, _payload.Length - offset);
                context.Response.OutputStream.Write(_payload, offset, count);
                context.Response.OutputStream.Flush();
                Thread.Sleep(30);
            }
        }

        public void Dispose() {
            _disposed = true;
            try {
                _listener.Stop();
            } catch {
            }
            try {
                _listener.Close();
            } catch {
            }
        }
    }

    private static void PumpUntil(Func<bool> condition, string message, int timeoutMs = PumpTimeoutMs) {
        var stopwatch = Stopwatch.StartNew();
        while (!condition()) {
            Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs, message);
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Net_request_downloads_text_from_local_server() {
        using var server = new LocalServer(Array.Empty<byte>());
        var result = global::PCL.ModNet.NetRequestByClient($"{server.BaseUrl}/ok", MakeLog: false);
        Assert.Equal("hello-pcl", result);
    }

    [AvaloniaFact]
    public void Net_request_throws_on_http_error_status() {
        using var server = new LocalServer(Array.Empty<byte>());
        var ex = Assert.Throws<HttpRequestCodeException>(
            () => global::PCL.ModNet.NetRequestByClient($"{server.BaseUrl}/missing", MakeLog: false));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [AvaloniaFact]
    public void Net_request_retry_recovers_from_transient_error() {
        using var server = new LocalServer(Array.Empty<byte>());
        var result = global::PCL.ModNet.NetRequestByClientRetry($"{server.BaseUrl}/retry");
        Assert.Equal("retry-ok", result);
        Assert.True(server.RequestCount >= 2, $"重试未发生，实际请求数 {server.RequestCount}");
    }

    [AvaloniaFact]
    public void Net_file_downloads_to_local_path() {
        var payload = new byte[64 * 1024];
        new Random(1234).NextBytes(payload);
        using var server = new LocalServer(payload);
        var target = Path.Combine(global::PCL.ModBase.PathTemp, "NetFileTests", $"ok-{Guid.NewGuid():N}.bin");
        try {
            var file = new global::PCL.ModNet.NetFile(new[] { $"{server.BaseUrl}/file" }, target);
            var loader = new global::PCL.ModNet.LoaderDownload("网络层测试下载", new[] { file });
            loader.Start();
            PumpUntil(() => loader.State is global::PCL.ModBase.LoadState.Finished
                or global::PCL.ModBase.LoadState.Failed, $"下载未在限定时间内完成，当前状态 {loader.State}");
            Assert.Equal(global::PCL.ModBase.LoadState.Finished, loader.State);
            Assert.True(File.Exists(target), "下载完成后文件不存在：" + target);
            Assert.Equal(payload, File.ReadAllBytes(target));
        } finally {
            try {
                if (File.Exists(target)) File.Delete(target);
            } catch {
            }
        }
    }

    [AvaloniaFact]
    public void Net_file_download_can_be_canceled() {
        var payload = new byte[512 * 1024];
        new Random(4321).NextBytes(payload);
        using var server = new LocalServer(payload);
        var target = Path.Combine(global::PCL.ModBase.PathTemp, "NetFileTests", $"cancel-{Guid.NewGuid():N}.bin");
        try {
            var file = new global::PCL.ModNet.NetFile(new[] { $"{server.BaseUrl}/slow" }, target);
            var loader = new global::PCL.ModNet.LoaderDownload("网络层取消测试", new[] { file });
            loader.Start();
            PumpUntil(() => loader.State == global::PCL.ModBase.LoadState.Loading, "下载未进入加载状态");
            Thread.Sleep(300);
            loader.Cancel();
            PumpUntil(() => loader.State == global::PCL.ModBase.LoadState.Canceled, "下载未在限定时间内取消");
            Assert.False(File.Exists(target), "取消后不应生成目标文件：" + target);
        } finally {
            try {
                if (File.Exists(target)) File.Delete(target);
            } catch {
            }
        }
    }

}
