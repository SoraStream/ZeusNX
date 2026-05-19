using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeusNX
{
    public class YYPatch
    {
        private static MethodDefinition FindMethod(AssemblyDefinition assembly, string targetName)
        {
            foreach (var type in assembly.MainModule.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (method.Name == targetName || method.FullName.Contains(targetName))
                        return method;
                }
            }
            return null;
        }

        private static void ResetMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.MaxStackSize = 8;
            method.Body.InitLocals = true;
        }

        public static void PatchAssetCompiler(AssemblyDefinition assetCompiler)
        {
            var mainModule = assetCompiler.MainModule;
            //CheckMakerInvokedMe()
            var method1 = FindMethod(assetCompiler, "CheckMakerInvokedMe");
            var processor = method1.Body.GetILProcessor();
            ResetMethod(method1);
            var method1type = method1.DeclaringType;

            var setLicense = method1type.Methods.First(m => m.Name == "set_LicenseValidForBuild");
            var getFeaturesEnable = method1type.Methods.First(m => m.Name == "get_FeatureFlagsEnable");
            var setFeaturesEnable = method1type.Methods.First(m => m.Name == "set_FeatureFlagsEnable");
            var getFeaturesDisable = method1type.Methods.First(m => m.Name == "get_FeatureFlagsDisable");
            var setFeaturesDisable = method1type.Methods.First(m => m.Name == "set_FeatureFlagsDisable");
            var setDefaultLimits = method1type.Methods.First(m => m.Name == "SetDefaultLimits");
            var limitsField = method1type.Fields.First(f => f.Name == "Limits");

            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Call, setLicense);
            processor.Emit(OpCodes.Call, getFeaturesEnable);
            var label17 = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Brtrue_S, label17);
            processor.Emit(OpCodes.Newobj, mainModule.ImportReference(typeof(System.Collections.Generic.Dictionary<string, string>).GetConstructor(Type.EmptyTypes)));
            processor.Emit(OpCodes.Call, setFeaturesEnable);
            processor.Append(label17);
            processor.Emit(OpCodes.Call, getFeaturesDisable);
            var label28 = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Brtrue_S, label28);
            processor.Emit(OpCodes.Newobj, mainModule.ImportReference(typeof(System.Collections.Generic.Dictionary<string, string>).GetConstructor(Type.EmptyTypes)));
            processor.Emit(OpCodes.Call, setFeaturesDisable);
            processor.Append(label28);
            processor.Emit(OpCodes.Call, setDefaultLimits);
            processor.Emit(OpCodes.Stsfld, limitsField);

            //dummy shit
            for (int i = 0; i < 8; i++)
                processor.Emit(OpCodes.Ldc_I4_0);
            for (int i = 0; i < 8; i++)
                processor.Emit(OpCodes.Pop);

            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Ret);
            processor = null;
            //IsFeatureEnabled(string feature)
            var method2 = FindMethod(assetCompiler, "IsFeatureEnabled");
            processor = method2.Body.GetILProcessor();
            ResetMethod(method2);

            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Ret);
            processor = null;

            //LimitsAllow(string catagory)
            var method3 = FindMethod(assetCompiler, "LimitsAllow");
            processor = method3.Body.GetILProcessor();
            ResetMethod(method3);

            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Ret);

            processor = null;

            //SetDefaultLimits()
            var method4 = FindMethod(assetCompiler, "SetDefaultLimits");
            processor = method4.Body.GetILProcessor();
            ResetMethod(method4);
            var method4type = method4.DeclaringType;
            var dictType = typeof(Dictionary<string, object>);
            var dictCtor = mainModule.ImportReference(dictType.GetConstructor(Type.EmptyTypes));
            var dictAdd = mainModule.ImportReference(dictType.GetMethod("Add"));
            var int32Type = mainModule.TypeSystem.Int32;
            limitsField = method4type.Fields.First(f => f.Name == "Limits");
            void AddLimit(string key, int value)
            {
                processor.Emit(OpCodes.Dup);
                processor.Emit(OpCodes.Ldstr, key);

                if (value == 1)
                    processor.Emit(OpCodes.Ldc_I4_1);
                else
                    processor.Emit(OpCodes.Ldc_I4, value);

                processor.Emit(OpCodes.Box, int32Type);
                processor.Emit(OpCodes.Callvirt, dictAdd);
            }

            processor.Emit(OpCodes.Newobj, dictCtor);
            AddLimit("Sprite", 99999);
            AddLimit("Tilesets", 99999);
            AddLimit("Sounds", 99999);
            AddLimit("Paths", 99999);
            AddLimit("Scripts", 99999);
            AddLimit("Shaders", 1);
            AddLimit("Fonts", 99999);
            AddLimit("Timelines", 99999);
            AddLimit("Objects", 99999);
            AddLimit("Rooms", 99999);
            AddLimit("Datafiles", 99999);
            AddLimit("Extensions", 1);
            AddLimit("Configs", 1);
            AddLimit("TexturePageSize", 1);
            AddLimit("PackageCreation", 1);
            AddLimit("SourceControl", 1);
            AddLimit("Import", 1);
            AddLimit("Export", 1);
            AddLimit("SWF", 1);
            AddLimit("Spine", 1);
            AddLimit("TextureGroups", 1);
            AddLimit("AudioGroups", 1);
            processor.Emit(OpCodes.Stsfld, limitsField);
            processor.Emit(OpCodes.Ldsfld, limitsField);
            processor.Emit(OpCodes.Ret);
        }
    }
}
