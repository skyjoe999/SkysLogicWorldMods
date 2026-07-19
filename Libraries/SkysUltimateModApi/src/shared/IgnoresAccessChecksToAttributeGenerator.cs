using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Builder;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using HarmonyLib;

namespace SkysUltimateModApi.Shared;


public static class IgnoresAccessChecksToAttributeGenerator
{
    public static void AddToAssembly(string assemblyPath, IEnumerable<string> assemblyNames)
    {
        // (this code is adapted from the publicizer source code)
        var assembly = AssemblyDefinition.FromImage(PEImage.FromFile(assemblyPath), new ModuleReaderParameters());
        var module = assembly.ManifestModule ?? throw new NullReferenceException();
        module.MetadataResolver = new DefaultMetadataResolver(NoopAssemblyResolver.Instance);

        // Our goal here is to add the System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute attribute to the assembly 
        // and then add that attribute with all the relevant assembly names.
        var attributeReference = module.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "Attribute").ImportWith(module.DefaultImporter);
        var baseConstructorReference = attributeReference.CreateMemberReference(".ctor", MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)).ImportWith(module.DefaultImporter);

        var Type = new TypeDefinition(
            "System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.NotPublic | TypeAttributes.Sealed,
            attributeReference
        );
        module.TopLevelTypes.Add(Type);

        // We need to define a constructor for this attribute that take a sting as an argument.
        var argumentType = module.CorLibTypeFactory.String;
        var constructorDefinition = new MethodDefinition(".ctor",
            MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName | MethodAttributes.Public,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, argumentType)
        );
        Type.Methods.Add(constructorDefinition);

        // The body of the constructor just calls the base constructor.
        var body = constructorDefinition.CilMethodBody = new CilMethodBody(constructorDefinition);
        body.Instructions.Add(CilOpCodes.Ldarg_0);
        body.Instructions.Add(CilOpCodes.Call, baseConstructorReference);
        body.Instructions.Add(CilOpCodes.Ret);

        assemblyNames
            .Select(name => new CustomAttributeSignature([new CustomAttributeArgument(argumentType, name)]))
            .Select(argument => new CustomAttribute(constructorDefinition, argument))
            .Do(module.Assembly.CustomAttributes.Add);

        // Write back to disk (called FatalWrite in th publicizer source)
        var result = new ManagedPEImageBuilder().CreateImage(module);
        if (result.HasFailed)
            throw new("Sky was *really* hoping this would never be thrown...");

        using var fileStream = File.Create(assemblyPath);
        new ManagedPEFileBuilder().CreateFile(result.ConstructedImage).Write(new BinaryStreamWriter(fileStream));
    }

    // a little ironic that I need to copy this code because the publicizer made this private...
    public class NoopAssemblyResolver : IAssemblyResolver
    {
        public static NoopAssemblyResolver Instance { get; } = new();
        public AssemblyDefinition Resolve(AssemblyDescriptor assembly) => null;
        public void AddToCache(AssemblyDescriptor descriptor, AssemblyDefinition definition) { }
        public bool RemoveFromCache(AssemblyDescriptor descriptor) => false;
        public bool HasCached(AssemblyDescriptor descriptor) => false;
        public void ClearCache() { }
    }
}