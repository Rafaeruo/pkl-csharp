namespace Pkl.PklTypes;

public enum DataSizeUnit : long
{
    Bytes = 1,
    Kilobytes = 1000,
    Kibibytes = 1024,
    Megabytes = Kilobytes * 1000,
    Mebibytes = Kibibytes * 1024,
    Gigabytes = Megabytes * 1000,
    Gibibytes = Mebibytes * 1024,
    Terabytes = Gigabytes * 1000,
    Tebibytes = Gibibytes * 1024,
    Petabytes = Terabytes * 1000,
    Pebibytes = Tebibytes * 1024
}