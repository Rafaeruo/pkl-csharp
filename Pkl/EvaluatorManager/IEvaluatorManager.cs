using Pkl.Evaluation;
using Pkl.InternalMsgApi.Outgoing;

namespace Pkl.EvaluatorManager;

/// <summary>
/// EvaluatorManager provides a way to minimize the overhead of multiple evaluators.
/// 
/// For example, if calling into Pkl as a child process, using the manager will share the same
/// process for all created evaluators. In contrast, constructing multiple evaluators through
/// NewEvaluator will spawn one process per evaluator.
/// </summary> 
public interface IEvaluatorManager 
{
    /// <summary>
	/// Close closes the evaluator manager and all of its evaluators.
	///
	/// If running Pkl as a child process, closes all evaluators as well as the child process.
	/// If calling into Pkl through the C API, close all existing evaluators.
    /// </summary>   
	void Close(); // TODO explain what errors in remarks

	/// <summary>
    /// GetVersion returns the version of Pkl backing this evaluator manager.
    /// </summary>
	string GetVersion(); // TODO explain what errors in remarks

    /// <summary>
	/// NewEvaluator constructs an evaluator instance.
	///
	/// If calling into Pkl as a child process, the first time NewEvaluator is called, this will
	/// start the child process.
    /// </summary>
	Task<IEvaluator> NewEvaluator(EvaluatorOptions options);

    /// <summary>
	/// NewProjectEvaluator is an easy way to create an evaluator that is configured by the specified
	/// projectDir.
	///
	/// It is similar to running the `pkl eval` or `pkl test` CLI command with a set `--project-dir`.
	///
	/// When using project dependencies, they must first be resolved using the `pkl project resolve`
	/// CLI command.
    /// </summary>   
    IEvaluator NewProjectEvaluator(string projectDir, EvaluatorOptions options);

	/// <summary>
	/// TODO
	/// </summary>
	void Send(IOutgoingMessage outgoingMessage);
}