namespace Scout.Web.Domain;

public enum PositionStatus
{
    Discovered, Interested, RejectedByUser, Expired
}

public static class PositionStatusNames
{
    public static string DisplayName(this PositionStatus status) => status switch
    {
        PositionStatus.Discovered => "Discovered",
        PositionStatus.Interested => "Interested",
        PositionStatus.RejectedByUser => "Rejected by me",
        PositionStatus.Expired => "Expired",
        _ => "Unknown"
    };
}
