using System.Collections.Generic;
using System.Linq;

namespace MimicFacility.Pipeline
{
    public static class FolderDependencyResolver
    {
        public static DependencyResolutionResult Resolve(
            MapDefinition mapDef,
            SystemRegistry registry,
            IReadOnlyCollection<string> presentSystemIds)
        {
            var result = new DependencyResolutionResult();
            var visited = new HashSet<string>();
            var stack = new HashSet<string>();

            foreach (var id in mapDef.requiredSystems)
                Visit(id, registry, result, visited, stack);

            foreach (var id in result.RequiredSystems)
            {
                if (presentSystemIds.Contains(id))
                    result.PresentSystems.Add(id);
                else
                    result.MissingSystems.Add(id);
            }

            return result;
        }

        private static void Visit(
            string id,
            SystemRegistry registry,
            DependencyResolutionResult result,
            HashSet<string> visited,
            HashSet<string> stack)
        {
            if (stack.Contains(id)) { result.Issues.Add($"Circular dependency: {id}"); return; }
            if (visited.Contains(id)) return;

            stack.Add(id);

            var descriptor = registry.GetById(id);
            if (descriptor == null) { result.Issues.Add($"Unknown system: {id}"); return; }

            foreach (var dep in descriptor.Dependencies)
                Visit(dep, registry, result, visited, stack);

            result.RequiredSystems.Add(id);
            visited.Add(id);
            stack.Remove(id);
        }
    }
}
