using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Idle.Extensions;
using Idle.Serialization.Abstract;

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
            // Get attribute
            var attrib = containerInfo.GetCustomAttribute<IdlePropertyAttribute>();

            // If no attribute, skip
            if (attrib == null)
                return false;

            // Check Atom
            if (!atom.TryGetProperty(attrib.label, out Property property))
                return false;

            // If null
            if (
                Nullable.GetUnderlyingType(containerInfo.ContainerType) != null &&
                property.DataType == PropertyType.NULL)
            {
                containerInfo.SetValue(
                    obj,
                    null);
            }

            // If iterable
            else if (IsIterable(containerInfo.ContainerType))
            {
                Type itemType = GetIterableItemType(containerInfo.ContainerType);

                // If System built-in
                if (itemType.Namespace == "System")
                {
                    if (property.DataType == PropertyType.COLOR)
                        throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {itemType}");

                    if (property.DataType == PropertyType.ATOM)
                        throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType} => {containerInfo.ContainerType})");

                    containerInfo.SetValue(
                        obj,
                        ConvertEnumerable(
                            containerInfo.ContainerType,
                            property.Select(x => x.Value)));
                }

                // If custom class
                else
                {
                    // If color
                    if (property.DataType == PropertyType.COLOR)
                    {
                        if (itemType != typeof(Color))
                            throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {itemType}");

                        var iter = property.Select(x => x.Value);

                        containerInfo.SetValue(
                            obj,
                            ConvertEnumerable(
                                containerInfo.ContainerType,
                                iter));
                    }

                    else
                    {
                        if (property.DataType != PropertyType.ATOM)
                            throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType}[] => {containerInfo.ContainerType})");

                        var iter = property.Select(
                            x => Deserialize(itemType, (Atom)x.Value));

                        containerInfo.SetValue(
                            obj,
                            ConvertEnumerable(
                                containerInfo.ContainerType,
                                iter));
                    }
                }
            }

            // If single
            else
            {
                // If System built-in
                if (containerInfo.ContainerType.Namespace == "System")
                {
                    if (property.DataType == PropertyType.COLOR)
                        throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {containerInfo.ContainerType}");

                    if (property.DataType == PropertyType.ATOM || property.DataType == PropertyType.COLOR)
                        throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType} => {containerInfo.ContainerType})");

                    if (property.IsArray)
                        throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType}[] => {containerInfo.ContainerType})");

                    containerInfo.SetValue(
                        obj,
                        property[0].Value);
                }

                // If custom class
                else
                {
                    // If color
                    if (property.DataType == PropertyType.COLOR)
                    {
                        if (containerInfo.ContainerType != typeof(Color))
                            throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {containerInfo.ContainerType}");

                        containerInfo.SetValue(
                            obj,
                            property[0].Value);
                    }

                    else
                    {
                        if (property.DataType != PropertyType.ATOM)
                            throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType} => {containerInfo.ContainerType})");

                        if (property.IsArray)
                            throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType}[] => {containerInfo.ContainerType})");

                        containerInfo.SetValue(
                            obj,
                            Deserialize(containerInfo.ContainerType, (Atom)property[0].Value));
                    }
                }
            }

            return true;
        }

        private static bool PopulateFlag(object obj, Atom atom, ContainerInfo containerInfo)
        {
            // Get attribute
            IdleAbstractFlag attrib = containerInfo.GetCustomAttribute<IdleAbstractFlag>(true);

            // If no attribute, skip
            if (attrib == null)
                return false;

            // Prepare references
            Property property;
            Flag flag;

            // If is a Child Flag
            if (attrib is IdleChildFlagBase flagAttrib)
            {
                // Check Atom
                if (!atom.TryGetProperty(flagAttrib.label, out property))
                    return false;

                // Check Property
                if (!property.TryGetItem(flagAttrib.index, out Item item))
                    return false;

                // If Property array
                if (property.IsArray)
                    throw new Exception($"Cannot differentiate Child Flags when in Property Arrays ('{flagAttrib.label}')");

                // Check Flag
                if (!flagAttrib.TryGetFlag(item, out flag))
                    return false;
            }

            // If is an own Flag
            else if (attrib is IdleFlagBase atomFlagAttrib)
            {
                // Make Property itself
                property = atom.OwnProperty;

                // Check Flag
                if (!atomFlagAttrib.TryGetFlag(atom, out flag))
                    return false;
            }

            // Can't be neither
            else
                throw new NotImplementedException();

            // If iterable
            if (IsIterable(containerInfo.ContainerType))
                throw new Exception($"Flags ('{containerInfo.ContainerType}') cannot be iterable");

            // If custom class
            else if (containerInfo.ContainerType.Namespace != "System" && !containerInfo.ContainerType.IsEnum)
                throw new Exception($"Flags ('{containerInfo.ContainerType}') cannot be a custom class");

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
                        throw new Exception($"Enums ('{containerInfo.Name}') must be NUMBER or TEXT as input");

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
                            throw new Exception("Nameless Flags can only be string");
                        }

                        else
                        {
                            if (flag.Value.type != PropertyType.NUMBER)
                                throw new Exception($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {containerInfo.ContainerType})");

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
                            throw new Exception("Nameless Flags can only be string");
                        }

                        else
                        {

                            if (flag.Value.type != PropertyType.FRACTION)
                                throw new Exception($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {containerInfo.ContainerType})");

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
                            throw new Exception("Nameless Flags can only be string");
                        }

                        else
                        {
                            if (flag.Value.type != PropertyType.TEXT)
                                throw new Exception($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {containerInfo.ContainerType})");

                            char[] chArray = ((string)flag.Value.value).ToCharArray();

                            if (chArray.Length != 1)
                                throw new Exception($"Invalid data size for '{property.Label}:{flag.Name}' (Expected 1 : Got {chArray.Length})");

                            containerInfo.SetValue(
                                obj,
                                chArray[0]);
                        }

                        break;

                    // Boolean
                    case TypeCode.Boolean:

                        if (attrib.Nameless)
                        {
                            throw new Exception("Nameless Flags can only be string");
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
                                throw new Exception($"Mismatching types ({flag.Value.type} => {containerInfo.ContainerType})");
                        }

                        break;

                    default:

                        throw new NotImplementedException("Invalid type");
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
