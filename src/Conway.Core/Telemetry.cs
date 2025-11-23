// Author: Ian Cooper
// Date: 23 November 2025
// Notes: OpenTelemetry instrumentation for Conway's Game of Life

using System.Diagnostics;

namespace Conway.Core;

/// <summary>
/// Central telemetry configuration for Conway's Game of Life
/// </summary>
public static class Telemetry
{
    public const string ServiceName = "Conway.GameOfLife";
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// ActivitySource for Conway's Game of Life tracing
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);
}
