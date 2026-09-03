using System.Collections.Generic;
using System.Reflection.Emit;
using FancyPantsConsole;
using HarmonyLib;
using LogicLog;
using LogicWorld.Logging;

namespace SkysConsoleTweaks.Client;

[HarmonyPatch]
// Remove the stack trace when copying from the clipboard.
public static class ConsoleCopyPatch
{
    [HarmonyPatch(typeof(LoggerToFancyPantsConsole), nameof(LoggerToFancyPantsConsole.Log), [typeof(string), typeof(string), typeof(LogLevel)])]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CopyMessageToClipboardPatch(IEnumerable<CodeInstruction> instructions)
    {
        var method = typeof(IConsole).Method("CreateDefaultColoredMessage");
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(method)) // Just before we call the method to create the message.
            {
                yield return new(OpCodes.Pop); // just drop the stacktrace.
                yield return new(OpCodes.Ldstr, ""); // and load a blank string instead.
            }
            yield return instruction;
        }
    }
}
