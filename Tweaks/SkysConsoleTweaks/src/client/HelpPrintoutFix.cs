using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LICC;

namespace SkysConsoleTweaks.Client;

[HarmonyPatch]
public static class HelpPrintoutFix
{
    private static readonly Type BuiltInCommandsType = typeof(LConsole).Assembly.GetType("LICC.BuiltInCommands");

    [HarmonyPatch] // Stupid private classes </3
    public static class HelpPrintoutFixInner
    {
        [HarmonyTargetMethod] public static MethodInfo HelpMethod() => BuiltInCommandsType.Method("Help", []);
        [HarmonyTranspiler] public static IEnumerable<CodeInstruction> HelpOverrideInner(IEnumerable<CodeInstruction> instructions) => HelpOverride(instructions);
    }

    // Breaks up the help text into multiple text blocks.
    public static IEnumerable<CodeInstruction> HelpOverride(IEnumerable<CodeInstruction> instructions)
    {
        // This is the method that prints empty lines.
        var blankLine = typeof(LineWriter).Method("WriteLine", []);

        // We are later going to replace the old method with the new one.
        var oldBlock = BuiltInCommandsType.Method("WriteAssemblyCommands");
        var newBlock = typeof(HelpPrintoutFix).Method("WriteAssemblyCommandsInner");

        var list = instructions.ToList();

        // Find the first load instruction before LineWriter.End is called.
        // That will be the load for the address of the line writer.
        var endFunc = typeof(LineWriter).Method("End");
        var endIndex = list.FindIndex(instruction => instruction.Calls(endFunc));
        var load = list.Take(endIndex).Last(instruction => instruction.IsLdloc());

        foreach (var instruction in list)
        {
            // When it would call WriteAssemblyCommands:
            if (instruction.Calls(oldBlock))
            {
                // Call WriteAssemblyCommandsInner instead with a reference to the LineWriter.
                yield return load.Clone();
                yield return instruction.Clone(newBlock);
            }
            else if (!instruction.Calls(blankLine)) // Skip the blank lines.
                yield return instruction;
        }
    }

    public static void WriteAssemblyCommandsInner(int maxUsageLength, LineWriter _, string assemblyName, ref LineWriter writer)
    {
        writer.End();
        writer = LConsole.BeginLine();
        WriteAssemblyCommands(maxUsageLength, writer, assemblyName);
    }

    [HarmonyPatch("LICC.BuiltInCommands", "WriteAssemblyCommands")][HarmonyReversePatch] public static void WriteAssemblyCommands(int maxUsageLength, LineWriter writer, string assemblyName) => throw new NotImplementedException("It's a stub");
}
