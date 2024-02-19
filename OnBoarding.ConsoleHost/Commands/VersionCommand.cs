using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace OnBoarding.ConsoleHost.Commands;

[Export(typeof(ICommand))]
[Export(typeof(VersionCommand))]
sealed class VersionCommand : VersionCommandBase
{
}
