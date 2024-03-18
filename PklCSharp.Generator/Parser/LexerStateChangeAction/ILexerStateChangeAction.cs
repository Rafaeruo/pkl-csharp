namespace PklCSharp.Generator.Parser.LexerStateChangeAction;

public interface ILexerStateChangeAction
{
    public void Execute(LexerState state);
}
