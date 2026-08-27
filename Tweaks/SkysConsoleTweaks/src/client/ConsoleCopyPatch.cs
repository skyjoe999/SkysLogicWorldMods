using System.Collections.Generic;
using System.Reflection.Emit;
using FancyPantsConsole;
using HarmonyLib;

namespace SkysConsoleTweaks.Client;

[HarmonyPatch]
// Remove the stack trace when copying from the clipboard.
public static class ConsoleCopyPatch
{
    [HarmonyPatch(typeof(Message), "CopyMessageToClipboard")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CopyMessageToClipboardPatch(IEnumerable<CodeInstruction> instructions)
    {
        // This is the field that contains the stack trace.
        var field = typeof(Message).Assembly.GetType("FancyPantsConsole.MessageData").Field("messageExtraText");
        foreach (var instruction in instructions)
        {
            if (instruction.LoadsField(field)) // When it would be loaded.
            {
                yield return new(OpCodes.Pop); // just drop the extra reference to message data.
                yield return new(OpCodes.Ldstr, ""); // and load a blank string instead.
            }
            else
                yield return instruction;
        }
    }
}
