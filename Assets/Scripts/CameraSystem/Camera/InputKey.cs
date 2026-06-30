public enum GameInputMode
{
    None,
    TopView,
    Character,
    UIOnly
}
public static class InputMapKeys
{
    public const string TopView = "TopView";
    public const string Player = "Player";
    public const string UI = "UI";
}

public static class InputActionKeys
{
    public const string Move = "Move";
    public const string Look = "Look";

    public const string PointerPosition = "PointerPosition";
    public const string PointerDelta = "PointerDelta";
    public const string Scroll = "Scroll";

    public const string PrimaryClick = "PrimaryClick";
    public const string SecondaryClick = "SecondaryClick";
    public const string MiddleClick = "MiddleClick";

    public const string TowerSelect = "TowerSelect";

    public const string Submit = "Submit";
    public const string Cancel = "Cancel";
}