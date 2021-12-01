using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Idle.Exceptions;
using Idle.Extensions;
using Idle.Serialization.Abstract;
using System.Collections;

namespace Idle.Serialization
{
    public static class IdleSerializer
    {
        private static BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static object Deserialize(Type type, IdleReader reader)
        {
            return Deserialize(type, reader.HeadAtom);
        }

        public static object Deserialize(Type type, Atom atom)
        {
            object obj = Activator.CreateInstance(type);

            // Fill fields
            foreach (FieldInfo fieldInfo in type.GetFields(Flags))
            {
                if (!PopulateProperty(obj, atom, fieldInfo))
                    PopulateFlag(obj, atom, fieldInfo);
            }

            // Fill properties
            foreach (PropertyInfo propertyInfo in type.GetProperties(Flags))
            {
                if (!PopulateProperty(obj, atom, propertyInfo))
                    PopulateFlag(obj, atom, propertyInfo);
            }

            return obj;
        }

        public static T Deserialize<T>(IdleReader reader)
        {
            return (T)Deserialize(typeof(T), reader.HeadAtom);
        }

        public static T Deserialize<T>(Atom atom)
        {
            return (T)Deserialize(typeof(T), atom);
        }

        private static bool PopulateProperty(object obj, Atom atom, ContainerInfo containerInfo)
        {
            var attrib = containerInfo.GetCustomAttribute<IdlePropertyAttribute>();

            if (attrib == null)
                return false;

            if (!atom.TryGetProperty(attrib.label, out Property property))
                return false;

            containerInfo.SetValue(
                obj,
                GetObjectData(property, containerInfo.ContainerType));

            return true;
        }

        private static object GetObjectData(Property property, Type containerType)
        {
            // If null
            if (
                property.DataType == PropertyType.NULL && (
                    Nullable.GetUnderlyingType(containerType) != null ||
                    containerType.Namespace != "System"
                ))
            {
                return null;
            }

            // If Dictionary
            else if (typeof(IDictionary).IsAssignableFrom(containerType))
            {
                if (property.DataType != PropertyType.ATOM)
                    throw new MismatchingTypeException(property, containerType);

                var main = (Atom)property[0].Value;

                PropertyType? valueType = null;

                var instance = (IDictionary)Activator.CreateInstance(containerType);

                foreach (var item in main.Properties)
                {
                    if (valueType == null)
                        valueType = item.DataType;

                    else if (valueType != item.DataType)
                        throw new DictionaryValueTypeException(property, valueType.Value, item.DataType);

                    instance.Add(
                        item.Label,
                        GetObjectData(item, containerType.GetIDictionaryValueType()));
                }

                return instance;
            }

            // If iterable
            else if (IsIterable(containerType))
            {
                Type itemType = GetIterableItemType(containerType);

                // If System built-in
                if (itemType.Namespace == "System")
                {
                    if (property.DataType == PropertyType.COLOR)
                        throw new ColorSerializationException(property, itemType);

                    if (property.DataType == PropertyType.ATOM)
                        throw new MismatchingTypeException(property, containerType);

                    return ConvertEnumerable(
                        containerType,
                        property.Select(x => x.Value));
                }

                // If custom class
                else
                {
                    // If color
                    if (property.DataType == PropertyType.COLOR)
                    {
                        if (itemType != typeof(Color))
                            throw new ColorSerializationException(property, itemType);

                        return ConvertEnumerable(
                            containerType,
                            property.Select(x => x.Value));
                    }

                    // If enum
                    else if (itemType.IsEnum)
                    {
                        Type enumType = Enum.GetUnderlyingType(itemType);

                        // Parse text or int
                        if (property.DataType == PropertyType.TEXT || property.DataType == PropertyType.NUMBER)
                        {
                            var a = property
                                    .Select(x => EnumExtensions.ParseOrNull(enumType, x.Value)).ToArray();

                            return ConvertEnumerable(
                                containerType,
                                property
                                    .Select(x => EnumExtensions.ParseOrNull(enumType, x.Value))
                                    .Where(x => x != null));
                        }

                        else
                            throw new EnumTypeException(containerType);

                    }

                    else
                    {
                        if (property.DataType != PropertyType.ATOM)
                            throw new MismatchingTypeException(property, containerType);

                        var iter = property.Select(
                            x => Deserialize(itemType, (Atom)x.Value));

                        return ConvertEnumerable(
                            containerType,
                            iter);
                    }
                }
            }

            // If single
            else
            {
                // If System built-in
                if (containerType.Namespace == "System")
                {
                    // If generic
                    if (containerType == typeof(object))
                    {
                        if (property.IsArray)
                            return property.Select(x => x.Value).ToArray();

                        else
                            return property[0].Value;
                    }

                    if (property.DataType == PropertyType.COLOR)
                        throw new ColorSerializationException(property, containerType);

                    if (property.DataType == PropertyType.ATOM || property.DataType == PropertyType.COLOR || property.IsArray)
                        throw new MismatchingTypeException(property, containerType);

                    return property[0].Value;
                }

                // If enum
                else if (containerType.IsEnum)
                {
                    Type enumType = Enum.GetUnderlyingType(containerType);

                    // Parse text
                    if (property.DataType == PropertyType.TEXT)
                    {
                        if (Enum.TryParse(containerType, property[0].Value.ToString(), true, out object result))
                            return result;

                        else
                            return Convert.ChangeType(0, enumType);
                    }

                    // Parse int
                    else if (property.DataType == PropertyType.NUMBER)
                    {
                        return Convert.ChangeType(property[0].Value, enumType);
                    }

                    else
                        throw new EnumTypeException(containerType);

                }

                // If custom class
                else
                {
                    // If color
                    if (property.DataType == PropertyType.COLOR)
                    {
                        if (containerType != typeof(Color))
                            throw new ColorSerializationException(property, containerType);

                        return property[0].Value;
                    }

                    else
                    {
                        if (property.DataType != PropertyType.ATOM || property.IsArray)
                            throw new MismatchingTypeException(property, containerType);

                        return Deserialize(
                            containerType,
                            (Atom)property[0].Value);
                    }
                }
            }
        }

        private static bool PopulateFlag(object obj, Atom atom, ContainerInfo containerInfo)
        {
            IdleAbstractFlag attrib = containerInfo.GetCustomAttribute<IdleAbstractFlag>(true);

            if (attrib == null)
                return false;

            // Prepare references
            Property property;
            Flag flag;

            // If is a Child Flag
            if (attrib is IdleChildFlagBase flagAttrib)
            {
                if (!atom.TryGetProperty(flagAttrib.label, out property))
                    return false;

                if (!property.TryGetItem(flagAttrib.index, out Item item))
                    return false;

                if (property.IsArray)
                    throw new ChildFlagArrayException(flagAttrib);

                if (!flagAttrib.TryGetFlag(item, out flag))
                    return false;
            }

            // If is an own Flag
            else if (attrib is IdleFlagBase atomFlagAttrib)
            {
                // Make Property itself
                property = atom.OwnProperty;

                if (!atomFlagAttrib.TryGetFlag(atom, out flag))
                    return false;
            }

            else
                throw new NotImplementedException();

            if (IsIterable(containerInfo.ContainerType))
                throw new FlagIterableException(containerInfo.ContainerType);

            // If custom class
            else if (containerInfo.ContainerType.Namespace != "System" && !containerInfo.ContainerType.IsEnum)
                throw new FlagCustomClassException(containerInfo.ContainerType);

            // Enum
            else if (containerInfo.ContainerType.IsEnum)
            {
                Type enumType = Enum.GetUnderlyingType(containerInfo.ContainerType);

                if (attrib.Nameless)
                {
                    if (Enum.TryParse(containerInfo.ContainerType, flag.Name, true, out object result))
                        containerInfo.SetValue(
                            obj,
                            result);

                    else
                        containerInfo.SetValue(
                            obj,
                            Convert.ChangeType(0, enumType));
                }

                else
                {
                    // Parse text
                    if (flag.Value.type == PropertyType.TEXT)
                    {
                        if (Enum.TryParse(containerInfo.ContainerType, flag.Value.value.ToString(), true, out object result))
                            containerInfo.SetValue(
                                obj,
                                result);

                        else
                            containerInfo.SetValue(
                                obj,
                                Convert.ChangeType(0, enumType));
                    }

                    // Parse int
                    else if (flag.Value.type == PropertyType.NUMBER)
                    {
                        containerInfo.SetValue(
                            obj,
                            Convert.ChangeType(flag.Value.value, enumType));
                    }

                    else
                        throw new EnumTypeException(containerInfo);

                }

            }

            // If null
            else if (
                Nullable.GetUnderlyingType(containerInfo.ContainerType) != null &&
                flag.Value.type == PropertyType.NULL)
            {
                containerInfo.SetValue(
                    obj,
                    null);
            }

            // Sytstem built-in
            else
            {
                switch (Type.GetTypeCode(containerInfo.ContainerType))
                {
                    // Integers
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Decimal:

                        if (attrib.Nameless)
                        {
                            throw new ShortFlagTypeException();
                        }

                        else
                        {
                            if (flag.Value.type != PropertyType.NUMBER)
                                throw new MismatchingFlagTypeException(property, flag, containerInfo.ContainerType);

                            containerInfo.SetValue(
                                obj,
                                (int)flag.Value.value);
                        }

                        break;

                    // Floating
                    case TypeCode.Single:
                    case TypeCode.Double:

                        if (attrib.Nameless)
                        {
                            throw new ShortFlagTypeException();
                        }

                        else
                        {

                            if (flag.Value.type != PropertyType.FRACTION)
                                throw new MismatchingFlagTypeException(property, flag, containerInfo.ContainerType);

                            containerInfo.SetValue(
                                obj,
                                (float)flag.Value.value);

                        }

                        break;

                    // String
                    case TypeCode.String:

                        if (attrib.Nameless)
                        {
                            containerInfo.SetValue(
                                obj,
                                flag.Name);
                        }

                        else
                        {
                            if (flag.Value.type == PropertyType.UNSET)
                                containerInfo.SetValue(
                                    obj,
                                    flag.Name.ToString());

                            else
                                containerInfo.SetValue(
                                    obj,
                                    flag.Value.value.ToString());
                        }

                        break;

                    // Single char
                    case TypeCode.Char:

                        if (attrib.Nameless)
                        {
                            throw new ShortFlagTypeException();
                        }

                        else
                        {
                            if (flag.Value.type != PropertyType.TEXT)
                                throw new MismatchingFlagTypeException(property, flag, containerInfo.ContainerType);

                            char[] chArray = ((string)flag.Value.value).ToCharArray();

                            if (chArray.Length != 1)
                                throw new MismatchingDataSizeException(property, flag, chArray.Length);

                            containerInfo.SetValue(
                                obj,
                                chArray[0]);
                        }

                        break;

                    // Boolean
                    case TypeCode.Boolean:

                        if (attrib.Nameless)
                        {
                            throw new ShortFlagTypeException();
                        }

                        else
                        {
                            if (flag.Value.type == PropertyType.UNSET)
                            {
                                containerInfo.SetValue(
                                    obj,
                                    !flag.Negated);
                            }

                            else if (flag.Value.type == PropertyType.BOOLEAN)
                            {
                                containerInfo.SetValue(
                                    obj,
                                    flag.Value.value);
                            }

                            else
                                throw new MismatchingFlagTypeException(property, flag, containerInfo.ContainerType);
                        }

                        break;

                    default:

                        throw new NotImplementedException();
                }

            }

            return true;
        }

        private static bool IsIterable(Type containerType)
        {
            return containerType.IsArray || (containerType != typeof(string) && containerType.IsEnumerator());
        }

        private static Type GetIterableItemType(Type containerType)
        {
            if (containerType.IsArray)
                return containerType.GetElementType();

            if (containerType.IsEnumerator())
            {
                var enumType = containerType
                    .GetInterfaces()
                    .Where(
                        t => t.IsGenericType &&
                        t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();

                return enumType ?? containerType;
            }

            throw new NotImplementedException();
        }

        private static dynamic ConvertEnumerable(Type containerType, IEnumerable<dynamic> obj)
        {
            Type itemType = GetIterableItemType(containerType);

            var objEnum = typeof(Enumerable)
                    .GetMethod("Cast")
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new object[] { obj });

            if (containerType.IsArray)

                return typeof(Enumerable)
                    .GetMethod("ToArray")
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new object[] { objEnum });

            if (containerType.IsList())

                return typeof(Enumerable)
                    .GetMethod("ToList")
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new object[] { objEnum });

            throw new NotImplementedException();
        }
    }
}
