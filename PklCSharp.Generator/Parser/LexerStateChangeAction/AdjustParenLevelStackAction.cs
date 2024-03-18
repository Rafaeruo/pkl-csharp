namespace PklCSharp.Generator.Parser.LexerStateChangeAction;

public class AdjustParenLevelStackAction: ILexerStateChangeAction
{
    private readonly bool _isPop;
    
    public AdjustParenLevelStackAction(bool isPop)
    {
        _isPop = isPop;
    }
    
    public void Execute(LexerState state)
    {
        if (_isPop)
        {
            state.ParenLevelScopes.Pop();
        }
        else
        {
            state.ParenLevelScopes.Push(0);
        }
    }
}
