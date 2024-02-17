using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using OmniReply.Utils;

namespace OmniReply.Core.CsScript
{
    public class MetadataResolver : MetadataReferenceResolver
    {
        static readonly ConcurrentBag<(AssemblyMetadata metadata, byte[] assembly)> globalResolvedAssemblies =
            new ConcurrentBag<(AssemblyMetadata, byte[])>();

        static readonly ConcurrentDictionary<string, Assembly> appdomainResolvedAssemblies = new ConcurrentDictionary<string, Assembly>();

        static readonly ConcurrentDictionary<ResolveReferenceRequest, ImmutableArray<PortableExecutableReference>> resolveReferenceCache =
            new(new ResolveReferenceRequestEqualityComparer());

        public MetadataResolver()
        {
        }

        public override bool Equals(object other) => this == other;

        public override int GetHashCode() => 0;


        struct ResolveReferenceRequest
        {
            public string reference;
            public string baseFilePath;
            public MetadataReferenceProperties properties;
        }

        class ResolveReferenceRequestEqualityComparer : IEqualityComparer<ResolveReferenceRequest>
        {
            public bool Equals(ResolveReferenceRequest x, ResolveReferenceRequest y) =>
                x.reference == y.reference &&
                x.baseFilePath == y.baseFilePath &&
                x.properties.Equals(y.properties);

            public int GetHashCode(ResolveReferenceRequest obj) => 0;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            //  Implement cache to avoid more memory usage (https://github.com/dotnet/roslyn/issues/33304)
            Log.WriteLog($"Resolving reference: {reference}, {baseFilePath}", Log.LogLevel.Debug);

            var request = new ResolveReferenceRequest
            {
                reference = reference,
                baseFilePath = baseFilePath,
                properties = properties
            };

            if (resolveReferenceCache.TryGetValue(request, out var resolvedReferences))
                return resolvedReferences;

            resolvedReferences = ResolveReferenceWithoutCache(request);

            resolveReferenceCache[request] = resolvedReferences;


            return resolvedReferences;
        }

        ImmutableArray<PortableExecutableReference> ResolveReferenceWithoutCache(ResolveReferenceRequest request)
        {
            return
                ScriptMetadataResolver.Default.ResolveReference(
                    request.reference,
                    request.baseFilePath,
                    request.properties);
        }

    }

}
