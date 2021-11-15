using System.Collections.Generic;

namespace Idle
{
    public class Atom
    {
        private readonly Dictionary<string, Property> m_properties;
        public IEnumerable<Property> Properties => m_properties.Values;

        public Atom()
        {
            m_properties = new Dictionary<string, Property>();
        }

        public Property CreateProperty(string name)
        {
            Property property = new Property(name);
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
    }
}
