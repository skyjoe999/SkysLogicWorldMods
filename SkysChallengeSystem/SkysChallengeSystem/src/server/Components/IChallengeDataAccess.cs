using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LogicAPI.Server.Components;

namespace SkysChallengeSystem.Server.Components;

public interface IChallengeDataAccess : ILogicUpdatable
{
    bool GetAnswer(int index);
    void SetQuestion(int index, bool value);

    IReadOnlyList<IChallengeDisplay> ChallengeDisplays { get; }
    void RegisterDisplay(IChallengeDisplay display);
    void RemoveDisplay(IChallengeDisplay display);
    void RegisterErrorDisplay(IChallengeErrorDisplay display);
    void RemoveErrorDisplay(IChallengeErrorDisplay display);
    void SetErrorMessage(string errorMessage);
    void ClearDisplays();

    bool[] GetAnswers(int answerCount) => Enumerable.Range(0, answerCount).Select(GetAnswer).ToArray();
    void ClearQuestions(int questionCount) => Enumerable.Range(0, questionCount).ForEach(i => SetQuestion(i, false));

    void SetDisplayMessage<M>(M message)
        => ChallengeDisplays.ForEach(display => (display as IChallengeDisplay<M>)?.SetValue(message));

    void SetDisplayMessageOf<T, M>(M message) where T : class, IChallengeDisplay<M>
        => ChallengeDisplays.ForEach(display => (display as T)?.SetValue(message));
}
