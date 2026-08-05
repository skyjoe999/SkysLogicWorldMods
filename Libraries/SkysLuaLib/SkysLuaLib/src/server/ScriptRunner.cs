// // If you want to use this component, you need to:
// // 1. uncomment this file
// // 2. uncomment the component file

// using System;
// using JimmysUnityUtilities;
// using LogicWorld.Server.Circuitry;
// using Lua;
// using Lua.Standard;
// using SkysLuaLib.Shared;

// namespace SkysLuaLib.Server;

// public class ScriptRunner : LogicComponent<ScriptRunner.IData>
// {
//     const string DefaultProgram =
//         """
//         using("LogicWorld.SharedCode")
//         using("JimmysUnityUtilities")

//         if not component.Inputs[1].On then return; end

//         -- Warning: Variables do not persist on reload
//         -- (I could use the On value of the output or custom data here but then I couldn't give the warning)
//         state = not state

//         component.Outputs[1].On = state

//         local color = nil
//         if state
//         then color = Colors.CircuitOn24
//         else color = Color24(0)
//         end

//         component.Data.LabelColor = color

//         print(color)
//         -- return color;
//         """;

//     public interface IData
//     {
//         string LabelText { get; set; }
//         Color24 LabelColor { get; set; }
//         bool LabelMonospace { get; set; }
//         float LabelFontSizeMax { get; set; }
//         int HorizontalAlignment { get; set; }
//         int VerticalAlignment { get; set; }
//         int SizeX { get; set; }
//         int SizeZ { get; set; }
//     }

//     protected override void SetDataDefaultValues()
//     {
//         Data.LabelText = DefaultProgram;
//         Data.LabelFontSizeMax = 0.8f;
//         Data.LabelColor = new Color24(38, 38, 38);
//         Data.LabelMonospace = true;
//         Data.HorizontalAlignment = 0;
//         Data.VerticalAlignment = 0;
//         Data.SizeX = 3;
//         Data.SizeZ = 2;
//     }

//     private LuaState state;
//     public Color24 LabelColor;

//     protected override void Initialize()
//     {
//         state = LuaState.Create();

//         state.Environment["component"] = new Wrapped(this);

//         state.Environment["Logger"] = LuaValue.FromObject(Logger);
//         state.Environment["using"] = UsingTypeLoader.UsingFunc;
//         state.OpenStandardLibraries();
//     }

//     protected override void DoLogicUpdate()
//     {
//         try
//         {
//             var results = state.DoStringAsync(Data.LabelText).AsTask().Result;
//             foreach (var result in results) Logger.Info(result.Pretty());
//         }
//         catch (Exception e)
//         {
//             Logger.Error("Uncaught Exception");
//             Logger.Exception(e);
//         }
//     }
// }
