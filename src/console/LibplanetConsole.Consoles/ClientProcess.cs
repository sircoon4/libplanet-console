using System.Diagnostics;
using System.Net;
using JSSoft.Terminals;
using Libplanet.Crypto;
using LibplanetConsole.Common;
using LibplanetConsole.Common.Extensions;
using static LibplanetConsole.Consoles.ProcessUtility;

namespace LibplanetConsole.Consoles;

internal sealed class ClientProcess : IDisposable
{
    private readonly Process _process;

    public ClientProcess(EndPoint endPoint, PrivateKey privateKey)
    {
        var startInfo = new ProcessStartInfo
        {
            ArgumentList =
            {
                "--end-point",
                EndPointUtility.ToString(endPoint),
                "--private-key",
                PrivateKeyUtility.ToString(privateKey),
                "--parent",
                Environment.ProcessId.ToString(),
            },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = IsWindows() ? ClientPath : "dotnet",
        };
        if (IsWindows() != true)
        {
            startInfo.ArgumentList.Insert(0, ClientPath);
        }

        _process = new Process
        {
            StartInfo = startInfo,
        };
        _process.ErrorDataReceived += Process_ErrorDataReceived;
        _process.Start();
    }

    public event EventHandler? Exited
    {
        add => _process.Exited += value;
        remove => _process.Exited -= value;
    }

    public int Id => _process.Id;

    public void Dispose()
    {
        if (_process.HasExited != true)
        {
            _process.Close();
        }
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is string text)
        {
            Console.Error.WriteColoredLine(text, TerminalColorType.Red);
        }
    }
}
