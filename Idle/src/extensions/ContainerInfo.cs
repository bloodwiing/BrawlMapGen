using System;
using System.Collections.Generic;
using System.Reflection;

namespace Idle.Extensions
{
    class ContainerInfo : MemberInfo
    {
        public ContainerInfo(FieldInfo fieldInfo)
        {
            m_fieldInfo = fieldInfo;
            m_original = fieldInfo;
            m_containerType = fieldInfo.FieldType;
        }

        public ContainerInfo(PropertyInfo propertyInfo)
        {
            m_propertyInfo = propertyInfo;
            m_original = propertyInfo;
            m_containerType = propertyInfo.PropertyType;
        }

        private FieldInfo m_fieldInfo = null;
        private PropertyInfo m_propertyInfo = null;

        private readonly MemberInfo m_original;


        public override Module Module => m_original.Module;
        public override int MetadataToken => m_original.MetadataToken;
        public override Type DeclaringType => m_original.DeclaringType;
        public override MemberTypes MemberType => m_original.MemberType;
        public override IEnumerable<CustomAttributeData> CustomAttributes => m_original.CustomAttributes;
        public override string Name => m_original.Name;
        public override Type ReflectedType => m_original.ReflectedType;

        public override object[] GetCustomAttributes(bool inherit)
        {
            return m_original.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_original.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_original.IsDefined(attributeType, inherit);
        }


        public static implicit operator ContainerInfo(FieldInfo fieldInfo)
        {
            return new ContainerInfo(fieldInfo);
        }

        public static implicit operator ContainerInfo(PropertyInfo propertyInfo)
        {
            return new ContainerInfo(propertyInfo);
        }


        private readonly Type m_containerType;
        public Type ContainerType => m_containerType;

        public void SetValue(object obj, object value)
        {
            if (m_fieldInfo != null)
                m_fieldInfo.SetValue(obj, value);
            else
                m_propertyInfo.SetValue(obj, value);
        }

        public T GetCustomAttribute<T>()
            where T : Attribute
        {
            if (m_fieldInfo != null)
                return m_fieldInfo.GetCustomAttribute<T>();
            else
                return m_propertyInfo.GetCustomAttribute<T>();
        }

        public T GetCustomAttribute<T>(bool inherit)
            where T : Attribute
        {
            if (m_fieldInfo != null)
                return m_fieldInfo.GetCustomAttribute<T>(inherit);
            else
                return m_propertyInfo.GetCustomAttribute<T>(inherit);
        }
    }
}
