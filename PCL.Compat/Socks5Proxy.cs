// SPDX-License-Identifier: Apache-2.0
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MeloongCore.Compat;

/// <summary>
/// SOCKS5（RFC 1928/1929）连接支持：为 SocketsHttpHandler 提供 ConnectCallback，
/// 让 HttpClient 能通过带认证的 SOCKS5 代理访问 HTTP/HTTPS。
/// </summary>
public static class Socks5Proxy {

    /// <summary>
    /// 创建 SOCKS5 代理处理器；certificateCallback 为可选的证书校验回调（用于登录等场景）。
    /// </summary>
    public static SocketsHttpHandler CreateHandler(string proxyHost, int proxyPort, string? username, string? password,
            Func<System.Net.Http.HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, SslPolicyErrors, bool>? certificateCallback = null) {
        var handler = new SocketsHttpHandler {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = false,
            ConnectCallback = CreateConnectCallback(proxyHost, proxyPort, username, password)
        };
        if (certificateCallback is not null)
            handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                certificateCallback(sender as System.Net.Http.HttpRequestMessage, cert as System.Security.Cryptography.X509Certificates.X509Certificate2, chain, errors);
        return handler;
    }

    /// <summary>
    /// 创建 SOCKS5 连接委托。
    /// </summary>
    public static Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> CreateConnectCallback(
            string proxyHost, int proxyPort, string? username, string? password) {
        return async (context, cancellationToken) => {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try {
                await socket.ConnectAsync(proxyHost, proxyPort, cancellationToken).ConfigureAwait(false);
                var stream = new NetworkStream(socket, ownsSocket: true);
                try {
                    await HandshakeAsync(stream, context.DnsEndPoint.Host, context.DnsEndPoint.Port,
                        username, password, cancellationToken).ConfigureAwait(false);
                    return stream;
                } catch {
                    stream.Dispose();
                    throw;
                }
            } catch {
                socket.Dispose();
                throw;
            }
        };
    }

    private static async Task HandshakeAsync(Stream stream, string targetHost, int targetPort,
            string? username, string? password, CancellationToken cancellationToken) {
        bool useAuth = !string.IsNullOrEmpty(username);
        // 1. 协商认证方式
        await stream.WriteAsync(useAuth ? new byte[] { 0x05, 0x02, 0x00, 0x02 } : new byte[] { 0x05, 0x01, 0x00 },
            cancellationToken).ConfigureAwait(false);
        var reply = await ReadExactlyAsync(stream, 2, cancellationToken).ConfigureAwait(false);
        if (reply[0] != 0x05) throw new IOException("SOCKS5 代理响应版本无效");
        switch (reply[1]) {
            case 0x00:
                break;
            case 0x02 when useAuth:
                // 2. 用户名/密码认证（RFC 1929）
                var userBytes = Encoding.UTF8.GetBytes(username!);
                var passBytes = Encoding.UTF8.GetBytes(password ?? "");
                if (userBytes.Length > 255 || passBytes.Length > 255) throw new IOException("SOCKS5 代理用户名或密码过长");
                var auth = new byte[3 + userBytes.Length + passBytes.Length];
                auth[0] = 0x01;
                auth[1] = (byte)userBytes.Length;
                userBytes.CopyTo(auth, 2);
                auth[2 + userBytes.Length] = (byte)passBytes.Length;
                passBytes.CopyTo(auth, 3 + userBytes.Length);
                await stream.WriteAsync(auth, cancellationToken).ConfigureAwait(false);
                var authReply = await ReadExactlyAsync(stream, 2, cancellationToken).ConfigureAwait(false);
                if (authReply[1] != 0x00) throw new IOException("SOCKS5 代理认证失败");
                break;
            default:
                throw new IOException($"SOCKS5 代理不接受当前认证方式（0x{reply[1]:X2}）");
        }
        // 3. CONNECT 请求
        byte[] address;
        var hostBytes = Encoding.UTF8.GetBytes(targetHost);
        if (IPAddress.TryParse(targetHost, out var ip)) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                address = new byte[1 + 4];
                address[0] = 0x01;
                ip.GetAddressBytes().CopyTo(address, 1);
            } else {
                address = new byte[1 + 16];
                address[0] = 0x04;
                ip.GetAddressBytes().CopyTo(address, 1);
            }
        } else {
            if (hostBytes.Length > 255) throw new IOException("SOCKS5 目标域名过长");
            address = new byte[2 + hostBytes.Length];
            address[0] = 0x03;
            address[1] = (byte)hostBytes.Length;
            hostBytes.CopyTo(address, 2);
        }
        var request = new byte[3 + address.Length + 2];
        request[0] = 0x05;
        request[1] = 0x01; // CONNECT
        request[2] = 0x00;
        address.CopyTo(request, 3);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(3 + address.Length), (ushort)targetPort);
        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        var head = await ReadExactlyAsync(stream, 4, cancellationToken).ConfigureAwait(false);
        if (head[1] != 0x00) throw new IOException($"SOCKS5 代理连接失败（错误码 0x{head[1]:X2}）");
        // 跳过绑定地址与端口
        int skip = head[3] switch {
            0x01 => 4 + 2,
            0x04 => 16 + 2,
            0x03 => (await ReadExactlyAsync(stream, 1, cancellationToken).ConfigureAwait(false))[0] + 2,
            _ => throw new IOException("SOCKS5 代理返回了不识别的地址类型")
        };
        await ReadExactlyAsync(stream, skip, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken cancellationToken) {
        var buffer = new byte[count];
        int offset = 0;
        while (offset < count) {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken).ConfigureAwait(false);
            if (read <= 0) throw new IOException("SOCKS5 代理连接被提前关闭");
            offset += read;
        }
        return buffer;
    }
}
