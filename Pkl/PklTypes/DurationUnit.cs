namespace Pkl.PklTypes;

public enum DurationUnit : long
{
    Nanosecond = 1,
    Microsecond = Nanosecond * 1000,
    Millisecond = Microsecond * 1000,
    Second = Millisecond * 1000,
    Minute = Second * 60,
    Hour = Minute * 60,
    Day = Hour * 24
}
