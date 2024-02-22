using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Pkl.Evaluation;
using Pkl.InternalMsgApi;
using Pkl.InternalMsgApi.Incoming;
using Pkl.InternalMsgApi.Outgoing;
using Pkl.StdOutput;

namespace Pkl.EvaluatorManager;

/// <inheritdoc/>
public class EvaluatorManager : IEvaluatorManager
{
    private const string semverPattern = @"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?";
    private static readonly Regex pklVersionRegex = new($"Pkl ({semverPattern}).*");
    private readonly string _pklCommand;
    private readonly string _pklCommandArgs;
    private readonly Process _process;
    private bool _closed;
    private string? _version;
    private Dictionary<long, Evaluator> _evaluators = [];
    private readonly Dictionary<long, TaskCompletionSource<CreateEvaluatorResponse>> _pendingEvaluators = [];
    private readonly StdOutputReader _stdoutReader;

    public EvaluatorManager() : this(Array.Empty<string>())
    {
    }

    public EvaluatorManager(string[] pklCommandParts)
    {
        (_pklCommand, _pklCommandArgs) = GetCommandAndArgStrings(pklCommandParts);
        (_process, _stdoutReader) = StartProcess();
    }

    private (string Cmd, string Args) GetCommandAndArgStrings(string[] pklCommandParts)
    {
        if (pklCommandParts is { Length: > 0 }) 
        {
            var cmd = pklCommandParts[0];
            var args = string.Join(' ', pklCommandParts, 1, pklCommandParts.Length - 1);
            return (cmd, args);
        }
    
        var pklExecEnv = Environment.GetEnvironmentVariable("PKL_EXEC");
        if (pklExecEnv is { Length: > 0 }) 
        {
            var envParts = pklExecEnv.Split(" ");
            var cmd = envParts[0];
            var args = string.Join(' ', envParts, 1, envParts.Length - 1);
            return (cmd, args);
        }

        return ("pkl", " ");
    }

    private (Process Process, StdOutputReader StdOutputReader) StartProcess() 
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _pklCommand,
                Arguments = _pklCommandArgs + " server",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdoutReader = new StdOutputReader(process.StandardOutput);
        stdoutReader.StandardOutputRead += OnStdoutDataReceived; 
        stdoutReader.Start();

        return (process, stdoutReader);
    }

    private void OnStdoutDataReceived(object? sender, StdoutReadEventArgs e)
    {
        var reader = new MessagePack.MessagePackReader(e.Stdout);
        var _ = reader.ReadArrayHeader();
        var code = reader.ReadInt32();

        var desserialized = MessagePack.MessagePackSerializer.Deserialize<IncomingMessageBase>(e.Stdout);

        // TODO support other codes
        switch(code)
        {
            case (int)Code.CodeNewEvaluatorResponse:
                var response = desserialized as CreateEvaluatorResponse;
                if (!_pendingEvaluators.TryGetValue(response!.RequestId, out var pending))
                {
                    // TODO
                    Console.WriteLine("warn: received a message for an unknown request id:" + response.RequestId);
                    return;
                }

                pending.SetResult(response);
                break;
            case (int)Code.CodeEvaluateResponse:
                var evaluateResponse = desserialized as EvaluateResponse;
                var evaluator = GetEvaluator(evaluateResponse!.EvaluatorId);

                evaluator?.HandleEvaluateResponse(evaluateResponse);
                break;
            default:
                throw new Exception("INVALID CODE");
        }
    }

    public void Close()
    {
        if (_closed)
        {
            return;
        }

        _stdoutReader.Stop();
        _process.Close();
        _closed = true;
    }

    public string GetVersion()
    {
        if (_version is not null) 
        {
            return _version;
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _pklCommand,
                Arguments = _pklCommandArgs + " --version",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdOutput = process.StandardOutput.ReadToEnd();
        var version = pklVersionRegex.Match(stdOutput);
        
        if (!version.Success || version.Groups.Count < 2) 
        {
            throw new InvalidOperationException($"failed to get version information from Pkl. Ran '{string.Join(" ", _pklCommandArgs)}', and got stdout \"{stdOutput}\"");
        }

        _version = version.Groups[1].Value;
        return _version;
    }

    public async Task<IEvaluator> NewEvaluator(EvaluatorOptions options)
    {
        if (_closed)
        {
            throw new InvalidOperationException("Evaluator manager is closed");
        }

        var createEvaluator = new CreateEvaluator
        {
            RequestId = 1, // TODO
            AllowedModules = options.AllowedModules.ToArray(),
            AllowedResources = options.AllowedResources.ToArray(),
            CacheDir = options.CacheDir,
            Env = options.Env,
            ModulePaths = options.ModulePaths is null ? [] :    options.ModulePaths.ToArray(),
            RootDir = options.RootDir,
            OutputFormat = options.OutputFormat,
            Properties = options.Properties,
            ClientModuleReaders = [], // todo
            ClientResourceReaders = [], // todo
            // Project = options.ProjectsDir ?? string.Empty // todo
        };

        var tcs = new TaskCompletionSource<CreateEvaluatorResponse>();
        _pendingEvaluators.Add(createEvaluator.RequestId, tcs);

        Send(createEvaluator);

        var createEvaluatorResponse = await tcs.Task;
        _pendingEvaluators.Remove(createEvaluator.RequestId);
        if (!string.IsNullOrEmpty(createEvaluatorResponse.Error)) 
        {
            throw new Exception(createEvaluatorResponse.Error);
        }

        var decoder = new Decoding.Decoder();
        var evaluator = new Evaluator(createEvaluatorResponse.EvaluatorId, this, decoder);

        _evaluators.Add(createEvaluatorResponse.EvaluatorId, evaluator);
        return evaluator;
    }

    public IEvaluator NewProjectEvaluator(string projectDir, EvaluatorOptions options)
    {
        throw new NotImplementedException();
    }

    public void Send(IOutgoingMessage outgoingMessage) 
    {
        var message = outgoingMessage.ToMsgPack();

        using var wtr = new BinaryWriter(_process.StandardInput.BaseStream, Encoding.UTF8, leaveOpen: true);
        wtr.Write(message);
    }

    private Evaluator? GetEvaluator(long evaluatorId)
    {
        if (_evaluators.TryGetValue(evaluatorId, out var evaluator))
        {
            return evaluator;
        }

        return null;
    }
}