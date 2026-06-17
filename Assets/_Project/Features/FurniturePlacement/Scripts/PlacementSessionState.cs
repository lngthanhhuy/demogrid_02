namespace SenCity.Features.FurniturePlacement
{
    public enum PlacementSessionState
    {
        Idle,
        PlacementNew,
        SelectedObject,
        MovingExisting,
        ValidPreview,
        InvalidPreview,
        RemoveConfirm,
        Saving,
        Error
    }
}
