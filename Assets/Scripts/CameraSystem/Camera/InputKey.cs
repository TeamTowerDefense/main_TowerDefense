public enum GameInputMode
{
    None,
    TopView,
    Character,
    UIOnly
}
public static class InputMapKeys
{
    public const string Common = nameof(Common);
    public const string TopView = nameof(TopView);
    public const string Player = nameof(Player);
    public const string UI = nameof(UI);
}

public static class InputActionKeys
{
    public const string Move = nameof(Move);
    public const string Look = nameof(Look);

    public const string PointerPosition = nameof(PointerPosition);
    public const string PointerDelta = nameof(PointerDelta);
    public const string Scroll = nameof(Scroll);

    public const string PrimaryClick = nameof(PrimaryClick);
    public const string SecondaryClick = nameof(SecondaryClick);
    public const string MiddleClick = nameof(MiddleClick);

    public const string TowerSelect = nameof(TowerSelect);

    public const string Submit = nameof(Submit);
    public const string Cancel = nameof(Cancel);

    public const string Number0 = nameof(Number0);
    public const string Number1 = nameof(Number1);
    public const string Number2 = nameof(Number2);
    public const string Number3 = nameof(Number3);
    public const string Number4 = nameof(Number4);
    public const string Number5 = nameof(Number5);
    public const string Number6 = nameof(Number6);
    public const string Number7 = nameof(Number7);
    public const string Number8 = nameof(Number8);
    public const string Number9 = nameof(Number9);
}

public static class InputNumberUtility
{
    public static bool TryGetNumber(string actionName, out int number)
    {
        number = actionName switch
        {
            InputActionKeys.Number1 => 0,
            InputActionKeys.Number2 => 1,
            InputActionKeys.Number3 => 2,
            InputActionKeys.Number4 => 3,
            InputActionKeys.Number5 => 4,
            InputActionKeys.Number6 => 5,
            InputActionKeys.Number7 => 6,
            InputActionKeys.Number8 => 7,
            InputActionKeys.Number9 => 8,
            InputActionKeys.Number0 => 9,
            _ => -1
        };

        return number >= 0;
    }
}