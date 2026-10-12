namespace ModelCollection.Models;

public enum CarCondition
{
    Mint,
    NearMint,
    Excellent,
    Good,
    Fair,
    PlayWorn,
}

public static class CarConditionExtensions
{
    public static string ToLabel(this CarCondition condition) => condition switch
    {
        CarCondition.NearMint => "Near mint",
        CarCondition.PlayWorn => "Play-worn",
        _ => condition.ToString(),
    };
}
