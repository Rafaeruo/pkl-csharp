namespace PklCSharp.Generator.Parser;

public class LexerState
{
    public int CurrentPosition { get; set; }
    public Stack<LexerMode> ModeStack { get; }
    public Stack<int> ParenLevelScopes { get; }
    public Stack<int> PoundLengthScopes { get; set; }

    public LexerMode CurrentMode => ModeStack.Peek();
    public int CurrentParenLevel => ParenLevelScopes.Peek();
    public int CurrentPoundLength => PoundLengthScopes.Peek();

    public LexerState()
    {
        CurrentPosition = 0;
        ModeStack = new Stack<LexerMode>();
        ParenLevelScopes = new Stack<int>();
        PoundLengthScopes = new Stack<int>();
        
        ModeStack.Push(LexerMode.Default);
        ParenLevelScopes.Push(0);
    }
}
