﻿using OmniReply.CommonUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using OmniReply.CommonUtils.SandBoxTransferTypes;

namespace OmniReply.CsSandBox.Core
{
    public class SandBox
    {
        private static List<Type> bannedType = new List<Type>
                            {
                                //not allow to use 
                                typeof(System.Reflection.Module),
                                typeof(System.AppDomain),
                                typeof(System.Diagnostics.Process),
                                typeof(System.Threading.Thread),
                                typeof(System.Environment)
                            };

        private List<Type> block_call_type;
        private ScriptState<object> _globalState;

        private ScriptOptions scriptOpt;
        private readonly MetadataReferenceResolver metadataResolver;

        public SandBox(InitRequest initData, SandBoxGlobals globals) : this(initData.InitCode, initData.References, bannedType, globals, initData.UsingNamespaces)
        {
            
        }

        public SandBox(string initCode, IEnumerable<string> references, List<Type> block_types, SandBoxGlobals globals, List<string> imports)
        {
            metadataResolver = new MetadataResolver();

            scriptOpt = ScriptOptions.Default;
            scriptOpt = scriptOpt.WithMetadataResolver(metadataResolver);

            var assembliesToAdd = new List<Assembly>();
            foreach (var reference in references)
            {
                Assembly loadedAssembly;

                try
                {
                    loadedAssembly = File.Exists(reference) ? Assembly.LoadFrom(reference) : Assembly.Load(reference);
                }
                catch (Exception e)
                {
                    Log.WriteLog($"Error when loading assembly: {reference}", Log.LogLevel.Error);
                    throw;
                }
                
                if (loadedAssembly != null)
                {
                    assembliesToAdd.Add(loadedAssembly);
                }
                else
                {
                    Log.WriteLog("Failed to load assembly: " + reference, Log.LogLevel.Warning);
                }
            }

            scriptOpt = scriptOpt.AddReferences(assembliesToAdd);
            scriptOpt = scriptOpt.WithImports(imports);

            scriptOpt = scriptOpt.WithAllowUnsafe(false);

            // Create initial script
            var script = CSharpScript.Create(initCode
            , scriptOpt, typeof(SandBoxGlobals));

            // Create an empty state
            _globalState = script.RunAsync
                (globals, _ => false, default).GetAwaiter().GetResult();

            block_call_type = block_types;
        }

        public async Task<object?> RunAsync(string code, CancellationToken token = default)
        {
            // Append the code to the last session
            var newScript = _globalState.Script
                .ContinueWith(code + Environment.NewLine, scriptOpt);

            // Diagnostics
            var diagnostics = newScript.Compile(token);
            foreach (var item in diagnostics)
            {
                if (item.Severity >= DiagnosticSeverity.Error)
                    throw new Exception(item.GetMessage());
            }

            Compilation compilation = newScript.GetCompilation();
            SyntaxTree syntaxTree = compilation.SyntaxTrees.Single();

            var syntaxNodeRoot = (CompilationUnitSyntax)syntaxTree.GetRoot();
            var model = compilation.GetSemanticModel(syntaxTree);

            // ~~Not~~ allow add new namespace
            // if (syntaxNodeRoot.Usings.Count > 0)
                // return null;

            // Get all call method symbol list
            var symbols = from node in syntaxNodeRoot.DescendantNodes()
                                        .OfType<InvocationExpressionSyntax>()
                          let symbol = model.GetSymbolInfo(node.Expression).Symbol as IMethodSymbol
                          where symbol != null
                          select symbol;

            foreach (var s in symbols)
            {
                foreach (var t in block_call_type)
                {
                    if (TypeSymbolMatchesType(s.ContainingType, t, model))
                        throw new Exception($"Now allowed symbol {s}.");
                }
            }


            // Execute the code
            _globalState = await newScript
                .RunFromAsync(_globalState, _ => false, token);


            return _globalState.ReturnValue;
        }

        static bool TypeSymbolMatchesType(ITypeSymbol typeSymbol, Type type, SemanticModel semanticModel)
        {
            var type_symbol = GetTypeSymbolForType(type, semanticModel);
            SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
            return comparer.Equals(typeSymbol, type_symbol);
        }

        static INamedTypeSymbol? GetTypeSymbolForType(Type type, SemanticModel semanticModel)
        {

            if (!type.IsConstructedGenericType)
            {
                if (type.FullName == null)
                {
                    return null;
                }

                return semanticModel.Compilation.GetTypeByMetadataName(type.FullName);
            }

            IEnumerable<INamedTypeSymbol> typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => GetTypeSymbolForType(a, semanticModel))!;

            var openType = type.GetGenericTypeDefinition();

            if (openType.FullName == null)
            {
                return null;
            }
            var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName);

            if (typeSymbol == null)
            {
                return null;
            }

            return typeSymbol.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>());
        }
    }
}
