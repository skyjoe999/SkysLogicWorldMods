namespace SkysChallengeSystem.Server.Components;

public interface IChallengeDisplay<in T> : IChallengeDisplay
{
    void IChallengeDisplay.SetValue(object value)
    {
        if (value is T _value) SetValue(_value);
    }

    void SetValue(T value);
}

public interface IChallengeErrorDisplay : IChallengeDisplay
{
    void SetError(string errorMessage);
    void ClearError();
}

public interface IChallengeDisplay
{
    void Clear();
    void SetValue(object value);
}