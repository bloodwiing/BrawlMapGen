using Idle.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Idle
{
    public class Property : IEnumerable<Item>
    {
        public readonly string Label;
        private readonly List<Item> m_items = new List<Item>();

        public PropertyType DataType = PropertyType.UNSET;

        public bool IsArray => m_items.Count > 1;
        public bool IsSingle => m_items.Count == 1;

        public Property(string label)
        {
            Label = label;
        }

        public void UpdateType(Item item)
        {
            if (DataType == PropertyType.UNSET)
                DataType = item.ValueType;
            else if (DataType != item.ValueType)
                throw new Exception($"Property List Type mismatch on '{Label}' ({DataType} != {item.ValueType})");
        }

        public Item NewItem()
        {
            Item item = new Item(this);
            m_items.Add(item);
            return item;
        }

        public Item GetItem(int index)
        {
            return m_items[index];
        }

        public bool TryGetItem(int index, out Item item)
        {
            if (index > m_items.Count + 1)
            {
                item = null;
                return false;
            }

            item = m_items[index];
            return true;
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return m_items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Item this[int index] => m_items[index];
    }

    public class Item
    {
        private readonly Property parent;

        private readonly Dictionary<string, Flag> m_flags = new Dictionary<string, Flag>();
        private Data m_value;

        public PropertyType ValueType => m_value.type;
        public object Value => m_value.value;

        public Item(Property parent)
        {
            this.parent = parent;
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

        public void SetValue(Data data)
        {
            m_value = data;
            parent.UpdateType(this);
        }
    }
}
