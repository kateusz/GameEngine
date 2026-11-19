namespace Editor.Features.Models;

public sealed class EditorLoadWorkMeter
{
    private int _total;
    private int _done;

    public int Total => Volatile.Read(ref _total);
    public int Done => Volatile.Read(ref _done);

    public float? Progress
    {
        get
        {
            var total = Total;
            if (total <= 0)
                return null;
            var done = Done;
            return done >= total ? 1f : done / (float)total;
        }
    }

    public void Reset()
    {
        Volatile.Write(ref _total, 0);
        Volatile.Write(ref _done, 0);
    }

    public void Expect(int units)
    {
        if (units > 0)
            Interlocked.Add(ref _total, units);
    }

    public void Advance(int units = 1)
    {
        if (units > 0)
            Interlocked.Add(ref _done, units);
    }

    public void FillJob(int expected, int advanced)
    {
        var left = expected - advanced;
        if (left > 0)
            Advance(left);
    }
}
