using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Sockets;

// redirect Console.Out to Console.Error
Console.SetOut(Console.Error);

// setup handler for Ctrl-Break and Ctrl-C
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
{
    cts.Cancel();
    e.Cancel = true;
};
var cancellationToken = cts.Token;
var cancellationTask = Task.Delay(-1, cancellationToken);

try
{
    if (OperatingSystem.IsWindows())
    {
        await NamedPipeProxy();
    }
    else
    {
        await SocketProxyServer();
    }
}
catch (OperationCanceledException)
{
    // ignore
}
finally
{
    cts.Cancel();
}

async Task NamedPipeProxy()
{
    // named pipe of ssh-agent.exe from Win32 OpenSSH
    var path = args.Length > 0 ? args[0] : @"openssh-ssh-agent";
    // connect to named pipe as client (enable in/out, async)
    using var pipe = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
    await pipe.ConnectAsync(TimeSpan.FromSeconds(5), cancellationToken);
    Console.Error.WriteLine($"connected to {path}");
    // stdio handles
    using var input = Console.OpenStandardInput();
    using var output = Console.OpenStandardOutput();
    // start reader and writer tasks
    await Task.WhenAny(
        StreamCopy("agent to proxy", pipe, output),
        StreamCopy("proxy to agent", input, pipe),
        cancellationTask);
}

async Task SocketProxyServer()
{
    // unix socket path 
    var path = args.Length > 0 ? args[0] : @"ssh-agent.socket";
    var endpoint = new UnixDomainSocketEndPoint(path);
    // create unix domain server socket
    using var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    // bind
    server.Bind(endpoint);
    // listen
    server.Listen(8);
    Console.Error.WriteLine($"listening on {path}");
    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    var connections = new List<Task>();
    var acceptTask = server.AcceptAsync(cancellationToken).AsTask();
    while (!cancellationToken.IsCancellationRequested)
    {
        var task = await Task.WhenAny(connections.Append(acceptTask).Append(cancellationTask));
        if (ReferenceEquals(task, acceptTask))
        {
            var socket = await acceptTask;
            //Console.Error.WriteLine($"accept() = {socket.RemoteEndPoint}");
            acceptTask = server.AcceptAsync(cancellationToken).AsTask();
            connections.Add(SocketProxy(socket));
        }
        else if (ReferenceEquals(task, cancellationTask))
        {
            return;
        }
        else
        {
            connections.Remove(task);
        }
    }
}

async Task SocketProxy(Socket socket)
{
    try
    {
        // ssh-agent proxy windows executable
        var path = args.Length > 1 ? args[1] : "./ssh-agent-wsl-proxy.exe";
        var info = new ProcessStartInfo
        {
            FileName = path,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(info);
        if (process is null) return;
        using var network = new NetworkStream(socket);
        await Task.WhenAny(
            process.WaitForExitAsync(cancellationToken),
            StreamCopy("client to proxy", network, process.StandardInput.BaseStream),
            StreamCopy("proxy to client", process.StandardOutput.BaseStream, network),
            cancellationTask);
        process.Kill();
    }
    finally
    {
        socket.Dispose();
    }
}

async Task StreamCopy(string name, Stream input, Stream output)
{
    var buffer = new byte[1024];
    while (true)
    {
        var n = await input.ReadAsync(buffer, cancellationToken);
        //Console.Error.WriteLine($"{name}: input.ReadAsync() = {n}");
        if (cancellationToken.IsCancellationRequested || n <= 0) break;
        await output.WriteAsync(buffer, 0, n);
    }
}
