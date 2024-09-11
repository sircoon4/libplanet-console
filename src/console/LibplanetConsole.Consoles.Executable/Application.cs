using JSSoft.Commands.Extensions;
using JSSoft.Terminals;
using LibplanetConsole.Common.Extensions;

namespace LibplanetConsole.Consoles.Executable;

internal sealed partial class Application(ApplicationOptions options)
    : ApplicationBase(options), IApplication
{
    private readonly ApplicationOptions _options = options;
    private Task _waitInputTask = Task.CompletedTask;

    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        if (_options.NoREPL != true)
        {
            var message = "Welcome to console for Libplanet.";
            var sw = new StringWriter();
            var commandContext = this.GetService<CommandContext>();
            var terminal = this.GetService<SystemTerminal>();
            commandContext.Out = sw;
            await Console.Out.WriteColoredLineAsync(message, TerminalColorType.BrightGreen);
            await base.OnRunAsync(cancellationToken);
            await sw.WriteSeparatorAsync(TerminalColorType.BrightGreen);
            await commandContext.ExecuteAsync(["--help"], cancellationToken: default);
            await sw.WriteLineAsync();
            await commandContext.ExecuteAsync(args: [], cancellationToken: default);
            await sw.WriteSeparatorAsync(TerminalColorType.BrightGreen);
            commandContext.Out = Console.Out;
            await Console.Out.WriteAsync(sw.ToString());
            await terminal.StartAsync(cancellationToken);
        }
        else
        {
            await base.OnRunAsync(cancellationToken);
            _waitInputTask = WaitInputAsync();
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        await _waitInputTask;
    }

    private async Task WaitInputAsync()
    {
        await Task.Run(() => Console.ReadKey(intercept: true));
        Cancel();
    }
}
