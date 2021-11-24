using System.Collections.Generic;
using System.Linq;

namespace Idle
{
    public class Atom
    {
        public readonly Atom Parent;
        public readonly Property OwnProperty;

        private readonly Dictionary<string, Property> m_properties;
        public IEnumerable<Property> Properties => m_properties.Values;

        private Dictionary<string, Flag> m_flags = new Dictionary<string, Flag>();

        public Atom(Atom parent, Property ownProperty)
        {
            Parent = parent;
            OwnProperty = ownProperty;

            m_properties = new Dictionary<string, Property>();
        }

        public Property CreateProperty(string name)
        {
            Property property = new Property(this, name);
            m_properties.Add(name, property);
            return property;
        }

        public Property GetProperty(string name)
        {
            return m_properties[name];
        }

        public bool TryGetProperty(string name, out Property property)
        {
            if (m_properties.ContainsKey(name))
            {
                property = m_properties[name];
                return true;
            }
            property = null;
            return false;
        }

        public bool HasProperty(string name)
        {
            return m_properties.ContainsKey(name);
        }

        public Property CreateOrGetProperty(string name)
        {
            if (TryGetProperty(name, out Property property))
                return property;
            return CreateProperty(name);
        }

        public Property this[string name] => m_properties[name];

        public void PassFlags(Dictionary<string, Flag> flags)
        {
            m_flags = flags;
        }

        public void SetFlag(Flag flag, bool? setNegated = null)
        {
            if (setNegated != null)
                flag.Negated = setNegated.Value;
            m_flags.Add(flag.Name, flag);
        }

        public Flag GetFlag(string name)
        {
            return m_flags[name];
        }

        public bool TryGetFlag(string name, out Flag flag)
        {
            return m_flags.TryGetValue(name, out flag);
        }

        public Flag GetFlag(int index)
        {
            return m_flags.ElementAt(index).Value;
        }

        public bool TryGetFlag(int index, out Flag flag)
        {
            if (index > m_flags.Count + 1)
            {
                flag = null;
                return false;
            }

            flag = m_flags.ElementAt(index).Value;
            return true;
        }
    }
}
