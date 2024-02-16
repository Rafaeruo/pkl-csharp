using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Pkl.Evaluator;
using Pkl.InternalMsgApi;
using Pkl.InternalMsgApi.Incoming;
using Pkl.InternalMsgApi.Outgoing;
using Pkl.Process1;

namespace Pkl.EvaluatorManager;

/// <inheritdoc/>
public class EvaluatorManager : IEvaluatorManager
{
    private const string semverPattern = @"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?";
    private static readonly Regex pklVersionRegex = new($"Pkl ({semverPattern}).*");

    private readonly string[] _pklCommand;
    private readonly Process _cmd;
    private bool _closed;
    private string? _version;

    private Dictionary<long, Evaluator.Evaluator> _evaluators = [];

    private readonly Dictionary<long, TaskCompletionSource<CreateEvaluatorResponse>> _pendingEvaluators = [];

    private readonly StdOutputReader stdoutReader;

    public EvaluatorManager(string[] pklCommand)
    {
        _pklCommand = pklCommand;

        _cmd = GetStartCommand();
        stdoutReader = new StdOutputReader(_cmd.StandardOutput);
        stdoutReader.StandardOutputRead += OnStdoutDataReceived; 
        
        stdoutReader.Start();
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

        stdoutReader.Stop();
        _cmd.Close();
        _closed = true;
    }

    public string GetVersion()
    {
        if (_version is not null) 
        {
            return _version;
        }

        var (cmd, args) = GetCommandAndArgStrings();
        var process = new Process 
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = string.Join(" ", [..args, "--version"]),
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var stdOutput = process.StandardOutput.ReadToEnd();

        var version = pklVersionRegex.Match(stdOutput);

        if (!version.Success || version.Groups.Count < 2) 
        {
            throw new Exception($"failed to get version information from Pkl. Ran '{string.Join(" ", args)}', and got stdout \"{stdOutput}\"");
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
        var evaluator = new Evaluator.Evaluator(createEvaluatorResponse.EvaluatorId, this, decoder);

        _evaluators.Add(createEvaluatorResponse.EvaluatorId, evaluator);
        return evaluator;
    }

    public IEvaluator NewProjectEvaluator(string projectDir, EvaluatorOptions options)
    {
        throw new NotImplementedException();
    }

    private Process GetStartCommand() 
    {
        var (cmd, args) = GetCommandAndArgStrings();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = string.Join(" ", [..args, "server"]),
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        return process;
    }

    private (string Cmd, IEnumerable<string> Args) GetCommandAndArgStrings() 
    {
        if (_pklCommand.Length > 0) 
        {
            return (_pklCommand.First(), new ArraySegment<string>(_pklCommand, 1, _pklCommand.Length - 1));
        }
    
        var pklExecEnv = Environment.GetEnvironmentVariable("PKL_EXEC");
        if (pklExecEnv is not null) 
        {
            var parts = pklExecEnv.Split(" ");

            return (parts.First(), new ArraySegment<string>(parts, 1, parts.Length - 1));
        }

        return ("pkl", []);
    }

    public void Send(IOutgoingMessage outgoingMessage) 
    {
        var message = outgoingMessage.ToMsgPack();

        using var wtr = new BinaryWriter(_cmd.StandardInput.BaseStream, Encoding.UTF8, leaveOpen: true);
        wtr.Write(message);
    }

    private Evaluator.Evaluator? GetEvaluator(long evaluatorId)
    {
        if (_evaluators.TryGetValue(evaluatorId, out var evaluator))
        {
            return evaluator;
        }

        return null;
    }
}