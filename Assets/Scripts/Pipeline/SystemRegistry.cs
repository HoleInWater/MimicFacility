using System.Collections.Generic;

namespace MimicFacility.Pipeline
{
    public class SystemRegistry
    {
        private Dictionary<string, SystemDescriptor> _systems = new Dictionary<string, SystemDescriptor>();

        public void Register(SystemDescriptor descriptor) =>
            _systems[descriptor.Id] = descriptor;

        public SystemDescriptor GetById(string id) =>
            _systems.TryGetValue(id, out var desc) ? desc : null;

        public IEnumerable<SystemDescriptor> All => _systems.Values;
    }
}
