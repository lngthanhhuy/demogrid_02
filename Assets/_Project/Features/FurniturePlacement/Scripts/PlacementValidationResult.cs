namespace SenCity.Features.FurniturePlacement
{
    public readonly struct PlacementValidationResult
    {
        public PlacementValidationResult(PlacementValidationFailure failure, string message)
        {
            Failure = failure;
            Message = message;
        }

        public PlacementValidationFailure Failure { get; }
        public string Message { get; }
        public bool IsValid => Failure == PlacementValidationFailure.None;

        public static PlacementValidationResult Valid()
        {
            return new PlacementValidationResult(PlacementValidationFailure.None, string.Empty);
        }

        public static PlacementValidationResult Invalid(PlacementValidationFailure failure, string message)
        {
            return new PlacementValidationResult(failure, message);
        }
    }

    public enum PlacementValidationFailure
    {
        None,
        NoActiveSession,
        MissingGrid,
        MissingItem,
        OutOfBounds,
        Overlap,
        Locked
    }
}
