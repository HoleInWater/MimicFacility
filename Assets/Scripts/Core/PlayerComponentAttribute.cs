using System;

namespace MimicFacility.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PlayerComponentAttribute : Attribute
    {
        public string Group { get; }
        public int Order { get; }
        public bool Optional { get; }

        public PlayerComponentAttribute(string group = "General", int order = 50, bool optional = false)
        {
            Group = group;
            Order = order;
            Optional = optional;
        }
    }
}
