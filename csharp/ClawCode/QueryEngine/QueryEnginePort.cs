using System.Text.Json;
using ClawCode.Commands;
using ClawCode.Models;
using ClawCode.Session;
using ClawCode.Subsystems;
using ClawCode.Tools;

namespace ClawCode.QueryEngine;

/// <summary>
/// C# 版轻量查询引擎，功能边界与 Python 工作台保持一致。
/// </summary>
public sealed class QueryEnginePort
{
    public PortManifest Manifest { get; }
    public QueryEngineConfig Config { get; set; }
    public string SessionId { get; }
    public List<string> MutableMessages { get; }
    public List<PermissionDenial> PermissionDenials { get; }
    public UsageSummary TotalUsage { get; private set; }
    public TranscriptStore TranscriptStore { get; }

    private QueryEnginePort(
        PortManifest manifest,
        QueryEngineConfig? config = null,
        string? sessionId = null,
        List<string>? mutableMessages = null,
        List<PermissionDenial>? permissionDenials = null,
        UsageSummary? totalUsage = null,
        TranscriptStore? transcriptStore = null)
    {
        Manifest = manifest;
        Config = config ?? new QueryEngineConfig();
        SessionId = sessionId ?? Guid.NewGuid().ToString("N");
        MutableMessages = mutableMessages ?? new List<string>();
        PermissionDenials = permissionDenials ?? new List<PermissionDenial>();
        TotalUsage = totalUsage ?? new UsageSummary();
        TranscriptStore = transcriptStore ?? new TranscriptStore();
    }

    public static QueryEnginePort FromWorkspace()
    {
        return new QueryEnginePort(PortManifestBuilder.Build());
    }

    public static QueryEnginePort FromSavedSession(string sessionId)
    {
        var stored = SessionStore.Load(sessionId);
        var transcript = new TranscriptStore();
        foreach (var message in stored.Messages)
        {
            transcript.Append(message);
        }

        transcript.Flush();
        return new QueryEnginePort(
            manifest: PortManifestBuilder.Build(),
            sessionId: stored.SessionId,
            mutableMessages: stored.Messages.ToList(),
            totalUsage: new UsageSummary(stored.InputTokens, stored.OutputTokens),
            transcriptStore: transcript);
    }

    public TurnResult SubmitMessage(
        string prompt,
        IReadOnlyList<string>? matchedCommands = null,
        IReadOnlyList<string>? matchedTools = null,
        IReadOnlyList<PermissionDenial>? deniedTools = null)
    {
        var commands = matchedCommands ?? Array.Empty<string>();
        var tools = matchedTools ?? Array.Empty<string>();
        var denials = deniedTools ?? Array.Empty<PermissionDenial>();

        if (MutableMessages.Count >= Config.MaxTurns)
        {
            var output = $"Max turns reached before processing prompt: {prompt}";
            return new TurnResult(
                Prompt: prompt,
                Output: output,
                MatchedCommands: commands,
                MatchedTools: tools,
                PermissionDenials: denials,
                Usage: TotalUsage,
                StopReason: "max_turns_reached");
        }

        var summaryLines = new[]
        {
            $"Prompt: {prompt}",
            $"Matched commands: {(commands.Count > 0 ? string.Join(", ", commands) : "none")}",
            $"Matched tools: {(tools.Count > 0 ? string.Join(", ", tools) : "none")}",
            $"Permission denials: {denials.Count}",
        };

        var outputText = FormatOutput(summaryLines);
        var projectedUsage = TotalUsage.AddTurn(prompt, outputText);
        var stopReason = projectedUsage.InputTokens + projectedUsage.OutputTokens > Config.MaxBudgetTokens
            ? "max_budget_reached"
            : "completed";

        MutableMessages.Add(prompt);
        TranscriptStore.Append(prompt);
        PermissionDenials.AddRange(denials);
        TotalUsage = projectedUsage;
        CompactMessagesIfNeeded();

        return new TurnResult(
            Prompt: prompt,
            Output: outputText,
            MatchedCommands: commands,
            MatchedTools: tools,
            PermissionDenials: denials,
            Usage: TotalUsage,
            StopReason: stopReason);
    }

    public IReadOnlyList<StreamEvent> BuildStreamEvents(string prompt, TurnResult result)
    {
        var events = new List<StreamEvent>
        {
            BuildEvent("message_start", new { session_id = SessionId, prompt }),
        };

        if (result.MatchedCommands.Count > 0)
        {
            events.Add(BuildEvent("command_match", new { commands = result.MatchedCommands }));
        }

        if (result.MatchedTools.Count > 0)
        {
            events.Add(BuildEvent("tool_match", new { tools = result.MatchedTools }));
        }

        if (result.PermissionDenials.Count > 0)
        {
            events.Add(BuildEvent("permission_denial", new
            {
                denials = result.PermissionDenials.Select(item => item.ToolName).ToArray()
            }));
        }

        events.Add(BuildEvent("message_delta", new { text = result.Output }));
        events.Add(BuildEvent("message_stop", new
        {
            usage = new { input_tokens = result.Usage.InputTokens, output_tokens = result.Usage.OutputTokens },
            stop_reason = result.StopReason,
            transcript_size = TranscriptStore.Entries.Count,
        }));

        return events;
    }

    public void CompactMessagesIfNeeded()
    {
        if (MutableMessages.Count > Config.CompactAfterTurns)
        {
            MutableMessages.RemoveRange(0, MutableMessages.Count - Config.CompactAfterTurns);
        }

        TranscriptStore.Compact(Config.CompactAfterTurns);
    }

    public IReadOnlyList<string> ReplayUserMessages()
    {
        return TranscriptStore.Replay();
    }

    public void FlushTranscript()
    {
        TranscriptStore.Flush();
    }

    public string PersistSession()
    {
        FlushTranscript();
        return SessionStore.Save(new StoredSession
        {
            SessionId = SessionId,
            Messages = MutableMessages.ToList(),
            InputTokens = TotalUsage.InputTokens,
            OutputTokens = TotalUsage.OutputTokens,
        });
    }

    public string RenderSummary()
    {
        var commandBacklog = CommandManager.BuildCommandBacklog();
        var toolBacklog = ToolManager.BuildToolBacklog();
        var lines = new List<string>
        {
            "# C# Porting Workspace Summary",
            "",
            Manifest.ToMarkdown(),
            "",
            $"Command surface: {commandBacklog.Modules.Count} mirrored entries",
        };

        lines.AddRange(commandBacklog.SummaryLines().Take(10));
        lines.Add("");
        lines.Add($"Tool surface: {toolBacklog.Modules.Count} mirrored entries");
        lines.AddRange(toolBacklog.SummaryLines().Take(10));
        lines.Add("");
        lines.Add($"Session id: {SessionId}");
        lines.Add($"Conversation turns stored: {MutableMessages.Count}");
        lines.Add($"Permission denials tracked: {PermissionDenials.Count}");
        lines.Add($"Usage totals: in={TotalUsage.InputTokens} out={TotalUsage.OutputTokens}");
        lines.Add($"Max turns: {Config.MaxTurns}");
        lines.Add($"Max budget tokens: {Config.MaxBudgetTokens}");
        lines.Add($"Transcript flushed: {TranscriptStore.Flushed}");
        return string.Join(Environment.NewLine, lines);
    }

    private string FormatOutput(IReadOnlyList<string> summaryLines)
    {
        if (!Config.StructuredOutput)
        {
            return string.Join(Environment.NewLine, summaryLines);
        }

        object payload = new
        {
            summary = summaryLines,
            session_id = SessionId,
        };

        Exception? lastError = null;
        for (var attempt = 0; attempt < Config.StructuredRetryLimit; attempt++)
        {
            try
            {
                return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
            {
                lastError = ex;
                payload = new
                {
                    summary = new[] { "structured output retry" },
                    session_id = SessionId,
                };
            }
        }

        throw new InvalidOperationException("structured output rendering failed", lastError);
    }

    private static StreamEvent BuildEvent(string type, object payload)
    {
        return new StreamEvent(type, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false
        }));
    }
}
