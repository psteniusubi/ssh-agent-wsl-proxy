using System.IO.Pipes;

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

// named pipe of ssh-agent.exe from Win32 OpenSSH
var path = @"openssh-ssh-agent";

// connect to named pipe as client (enable in/out, async)
using var pipe = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
try
{
    await pipe.ConnectAsync(cancellationToken);
    Console.Error.WriteLine(path);
    // start reader and writer tasks
    await Task.WhenAny(ConsoleToPipe(), PipeToConsole(), cancellationTask);
}
catch (OperationCanceledException)
{
    // ignore
}
finally
{
    cts.Cancel();
}

// read from console, write to pipe
async Task ConsoleToPipe()
{
    var buffer = new byte[1024];
    using var input = Console.OpenStandardInput();
    while (true)
    {
        var n = await input.ReadAsync(buffer, cancellationToken);
        Console.Error.WriteLine($"input.ReadAsync() = {n}");
        if (cts.IsCancellationRequested || n <= 0) break;
        await pipe.WriteAsync(buffer, 0, n);
        Console.Error.WriteLine($"pipe.WriteAsync({n})");
    }
    cts.Cancel();
}

// read from pipe, write to console
async Task PipeToConsole()
{
    var buffer = new byte[1024];
    using var output = Console.OpenStandardOutput();
    while (true)
    {
        var n = await pipe.ReadAsync(buffer, cancellationToken);
        Console.Error.WriteLine($"pipe.ReadAsync() = {n}");
        if (cts.IsCancellationRequested || n <= 0) break;
        await output.WriteAsync(buffer, 0, n);
        Console.Error.WriteLine($"output.WriteAsync({n})");
    }
    cts.Cancel();
}