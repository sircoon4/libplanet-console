﻿using System.ComponentModel;
using System.Text.Json.Serialization;
using JSSoft.Commands;
using LibplanetConsole.Common.DataAnnotations;
using LibplanetConsole.Frameworks;

namespace LibplanetConsole.Nodes.Explorer;

[ApplicationSettings]
internal sealed class ExplorerSettings
{
    public const string Explorer = nameof(Explorer);

    [CommandPropertySwitch("explorer")]
    [CommandSummary("")]
    [JsonPropertyName("isEnabled")]
    [Category(Explorer)]
    public bool IsExplorerEnabled { get; init; }

    [CommandProperty("explorer-end-point", DefaultValue = "")]
    [CommandSummary("")]
    [CommandPropertyDependency(nameof(IsExplorerEnabled))]
    [JsonPropertyName("endPoint")]
    [AppEndPoint]
    [Category(Explorer)]
    public string ExplorerEndPoint { get; init; } = string.Empty;
}
