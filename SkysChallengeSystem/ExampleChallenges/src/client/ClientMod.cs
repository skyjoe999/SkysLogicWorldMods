using System.Reflection;
using HarmonyLib;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld;
using LogicWorld.Building.Overhaul;
using LogicWorld.Building.Overhaul.Grabbing;
using SkysChallengeSystem.Client;

namespace ExampleChallenges.Client;

public class ExampleChallenges_ClientMod : ClientMod
{
    protected override void Initialize()
    {
        ChallengeManager.RegisterChallenges(Files, Manifest);
    }
}