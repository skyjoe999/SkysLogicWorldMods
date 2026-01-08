using System;
using JECS;
using LogicAPI.Modding;

namespace SkysChallengeSystem.Shared;

public record ChallengeRecord
{
    [DontSaveThis] public string Name { get; set; }
    [DontSaveThis] public ModManifest Mod { get; set; }
    [SaveThis] public string Description { get; set; }
    [SaveThis] public Version Version { get; set; }
    [SaveThis] public string Folder { get; set; } = "";
    [SaveThis] public string[] QuestionNames { get; set; } = [];
    [SaveThis] public string[] AnswerNames { get; set; } = [];
    public string FullPath => Folder + "/" + Name;
    public int QuestionCount => QuestionNames.Length;
    public int AnswerCount => AnswerNames.Length;

    protected virtual Type EqualityContract => typeof(ChallengeRecord);
}
