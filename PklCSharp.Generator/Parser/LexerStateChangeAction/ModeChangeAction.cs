using Microsoft.CodeAnalysis.Text;
using PklCSharp.Generator.Exceptions;

namespace PklCSharp.Generator.Parser.LexerStateChangeAction;

public class ModeChangeAction: ILexerStateChangeAction
{
    private readonly LexerMode _mode;
    private readonly LinePosition _position;
    
    public ModeChangeAction(LexerMode mode, LinePosition position)
    {
        _mode = mode;
        _position = position;
    }

    public void Execute(LexerState state)
    {
        if (_mode == LexerMode.Pop)
        {
            if (state.ModeStack.Count > 1)
            {
                state.ModeStack.Pop();
            }
            else
            {
                throw new LexerException("Trying to pop the base lexer mode.", _position);
            }
        }
        else
        {
            state.ModeStack.Push(_mode);
        }
    }
}
