using System.Collections;

namespace Pkl.PklTypes;

public struct IntSeq : IEnumerable<int>
{
    public IntSeq(int start, int end, int step)
    {
        Start = start;
        End = end;
        Step = step;
    }

    public int Start { get; set; }
    public int End { get; set; }
    public int Step { get; set; }

    public IEnumerator<int> GetEnumerator()
    {
        for (int i = Start; (Step > 0 && i <= End) || (Step < 0 && i >= End); i += Step)
        {
            yield return i;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
