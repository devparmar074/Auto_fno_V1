namespace AutoFno.Domain.Enums;

public enum BuildupType
{
    Neutral = 0,
    LongBuildup = 1,    // Price Up, OI Up (Bullish)
    ShortBuildup = 2,   // Price Down, OI Up (Bearish)
    ShortCovering = 3,  // Price Up, OI Down (Bullish short squeeze)
    LongUnwinding = 4   // Price Down, OI Down (Bearish profit booking / exit)
}
