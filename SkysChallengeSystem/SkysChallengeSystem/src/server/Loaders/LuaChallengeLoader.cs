// using System;
// using JECS;
// using Lua;
// using SkysChallengeSystem.Server.Challenges;
// using SkysChallengeSystem.Shared;

// namespace SkysChallengeSystem.Server.Loaders;

// public class LuaChallengeLoader : ChallengeLoader<LuaChallengeLoader.LuaChallengeRecord>
// {
//     public class LuaChallengeRecord : IChallengeRecord
//     {
//         [DontSaveThis] public string Name { get; set;}
//         [DontSaveThis] public string Mod { get; set; }
//         [SaveThis] public string Description { get; init;}
//         [SaveThis] public Version Version { get; init;}
//         [SaveThis] public string Folder { get; init;}
//         [SaveThis] public string Script { get; init;}
//     }

//     public override Challenge LoadChallenge(LuaChallengeRecord record)
//     {
//         var state = LuaState.Create();
//         var success = state.DoStringAsync(record.Script).AsTask().Result[0].TryRead<LuaTable>(out var result);

//         return new LuaChallenge(AsFunction(result, "OnBegin"), 
//             AsFunction(result, "OnStep"), 
//             AsFunction(result, "OnSuccess"), 
//             AsFunction(result, "OnFailure"), 
//             AsFunction(result, "OnDispose")
//             );

//         LuaFunction AsFunction(LuaTable table, string name) 
//             => table[new LuaValue(name)].TryRead(out LuaFunction func) ? func : null;
//     }

//     private class LuaChallenge(
//         LuaFunction onBegin,
//         LuaFunction onStep,
//         LuaFunction onSuccess,
//         LuaFunction onFailure,
//         LuaFunction dispose
//         ) : Challenge
//     {

//         protected override void OnBegin()
//         {
//             throw new NotImplementedException();
//         }

//         protected override bool OnStep()
//         {
//             throw new NotImplementedException();
//         }

//         protected override void OnSuccess()
//         {
//             throw new NotImplementedException();
//         }

//         protected override void OnFailure()
//         {
//             throw new NotImplementedException();
//         }

//         public override void Dispose()
//         {
//             throw new NotImplementedException();
//         }
//     }
// }