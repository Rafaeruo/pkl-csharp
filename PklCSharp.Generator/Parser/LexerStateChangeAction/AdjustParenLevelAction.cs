namespace PklCSharp.Generator.Parser.LexerStateChangeAction;

public class AdjustParenLevelAction: ILexerStateChangeAction
{
    private readonly int _diff;
    
    public AdjustParenLevelAction(int diff)
    {
        _diff = diff;
    }
    
    public void Execute(LexerState state)
    {
        state.ParenLevelScopes.Push(state.ParenLevelScopes.Pop() + _diff);
    }
}
