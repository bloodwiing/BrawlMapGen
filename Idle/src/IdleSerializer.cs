using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Idle.Extensions;

namespace Idle.Serialization
{
    public static class IdleSerializer
    {
        public static object Deserialize(Type type, IdleReader reader)
        {
            return Deserialize(type, reader.HeadAtom);
        }

        public static object Deserialize(Type type, Atom atom)
        {
            object obj = Activator.CreateInstance(type);

            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                if (!PopulateProperty(obj, atom, fieldInfo))
                    PopulateFlag(obj, atom, fieldInfo);
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

        private static bool PopulateProperty(object obj, Atom atom, FieldInfo fieldInfo)
        {
            // Get attribute
            var attrib = fieldInfo.GetCustomAttribute<IdlePropertyAttribute>();

            // If no attribute, skip
            if (attrib == null)
                return false;

            // Check Atom
            if (!atom.TryGetProperty(attrib.label, out Property property))
                return false;

            // If null
            if (
                Nullable.GetUnderlyingType(fieldInfo.FieldType) != null &&
                property.DataType == PropertyType.NULL)
            {
                fieldInfo.SetValue(
                    obj,
                    null);
            }

            // If iterable
            else if (IsIterable(fieldInfo.FieldType))
            {
                Type itemType = GetIterableItemType(fieldInfo.FieldType);

                // If System built-in
                if (itemType.Namespace == "System")
                {
                    if (property.DataType == PropertyType.COLOR)
                        throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {itemType}");

                    if (property.DataType == PropertyType.ATOM)
                        throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType} => {fieldInfo.FieldType})");

                    fieldInfo.SetValue(
                        obj,
                        ConvertEnumerable(
                            fieldInfo.FieldType,
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

                        fieldInfo.SetValue(
                            obj,
                            ConvertEnumerable(
                                fieldInfo.FieldType,
                                iter));
                    }

                    else
                    {
                        if (property.DataType != PropertyType.ATOM)
                            throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType}[] => {fieldInfo.FieldType})");

                        var iter = property.Select(
                            x => Deserialize(itemType, (Atom)x.Value));

                        fieldInfo.SetValue(
                            obj,
                            ConvertEnumerable(
                                fieldInfo.FieldType,
                                iter));
                    }
                }
            }

            // If single
            else
            {
                // If System built-in
                if (fieldInfo.FieldType.Namespace == "System")
                {
                    if (property.DataType == PropertyType.COLOR)
                        throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {fieldInfo.FieldType}");

                    if (property.DataType == PropertyType.ATOM || property.DataType == PropertyType.COLOR)
                        throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType} => {fieldInfo.FieldType})");

                    if (property.IsArray)
                        throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType}[] => {fieldInfo.FieldType})");

                    fieldInfo.SetValue(
                        obj,
                        property[0].Value);
                }

                // If custom class
                else
                {
                    // If color
                    if (property.DataType == PropertyType.COLOR)
                    {
                        if (fieldInfo.FieldType != typeof(Color))
                            throw new Exception($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {fieldInfo.FieldType}");

                        fieldInfo.SetValue(
                            obj,
                            property[0].Value);
                    }

                    else
                    {
                        if (property.DataType != PropertyType.ATOM)
                            throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType} => {fieldInfo.FieldType})");

                        if (property.IsArray)
                            throw new Exception($"Mismatching types for '{property.Label}' ({property.DataType}[] => {fieldInfo.FieldType})");

                        fieldInfo.SetValue(
                            obj,
                            Deserialize(fieldInfo.FieldType, (Atom)property[0].Value));
                    }
                }
            }

            return true;
        }

        private static bool PopulateFlag(object obj, Atom atom, FieldInfo fieldInfo)
        {
            // Get attribute
            IdleFlagBase attrib = fieldInfo.GetCustomAttribute<IdleFlagAttribute>();

            // Get attribute #2 if failed
            if (attrib == null)
                attrib = fieldInfo.GetCustomAttribute<IdleNamelessFlagAttribute>();

            // If no attribute, skip
            if (attrib == null)
                return false;

            // Check Atom
            if (!atom.TryGetProperty(attrib.label, out Property property))
                return false;

            // Check Property
            if (!property.TryGetItem(attrib.index, out Item item))
                return false;

            // Check Flag
            if (!attrib.TryGetFlag(item, out Flag flag))
                return false;

            // If iterable
            if (IsIterable(fieldInfo.FieldType))
                throw new Exception($"Flags ('{fieldInfo.FieldType}') cannot be iterable");

            // If custom class
            else if (fieldInfo.FieldType.Namespace != "System" && !fieldInfo.FieldType.IsEnum)
                throw new Exception($"Flags ('{fieldInfo.FieldType}') cannot be a custom class");

            // If array
            else if (property.IsArray)
                throw new Exception($"Mismatching types ({flag.Value.type} => {fieldInfo.FieldType})");

            // Enum
            else if (fieldInfo.FieldType.IsEnum)
            {
                Type enumType = Enum.GetUnderlyingType(fieldInfo.FieldType);

                if (attrib.Nameless)
                {
                    if (Enum.TryParse(fieldInfo.FieldType, flag.Name, true, out object result))
                        fieldInfo.SetValue(
                            obj,
                            result);

                    else
                        fieldInfo.SetValue(
                            obj,
                            Convert.ChangeType(0, enumType));
                }

                else
                {
                    // Parse text
                    if (flag.Value.type == PropertyType.TEXT)
                    {
                        if (Enum.TryParse(fieldInfo.FieldType, flag.Value.value.ToString(), true, out object result))
                            fieldInfo.SetValue(
                                obj,
                                result);

                        else
                            fieldInfo.SetValue(
                                obj,
                                Convert.ChangeType(0, enumType));
                    }

                    // Parse int
                    else if (flag.Value.type == PropertyType.NUMBER)
                    {
                        fieldInfo.SetValue(
                            obj,
                            Convert.ChangeType(flag.Value.value, enumType));
                    }

                    else
                        throw new Exception($"Enums ('{fieldInfo.Name}') must be NUMBER or TEXT as input");

                }

            }

            // If null
            else if (
                Nullable.GetUnderlyingType(fieldInfo.FieldType) != null &&
                flag.Value.type == PropertyType.NULL)
            {
                fieldInfo.SetValue(
                    obj,
                    null);
            }

            // Sytstem built-in
            else
            {
                switch (Type.GetTypeCode(fieldInfo.FieldType))
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
                                throw new Exception($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {fieldInfo.FieldType})");

                            fieldInfo.SetValue(
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
                                throw new Exception($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {fieldInfo.FieldType})");

                            fieldInfo.SetValue(
                                obj,
                                (float)flag.Value.value);

                        }

                        break;

                    // String
                    case TypeCode.String:

                        if (attrib.Nameless)
                        {
                            fieldInfo.SetValue(
                                obj,
                                flag.Name);
                        }

                        else
                        {
                            if (flag.Value.type == PropertyType.UNSET)
                                fieldInfo.SetValue(
                                    obj,
                                    flag.Name.ToString());

                            else
                                fieldInfo.SetValue(
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
                                throw new Exception($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {fieldInfo.FieldType})");

                            char[] chArray = ((string)flag.Value.value).ToCharArray();

                            if (chArray.Length != 1)
                                throw new Exception($"Invalid data size for '{property.Label}:{flag.Name}' (Expected 1 : Got {chArray.Length})");

                            fieldInfo.SetValue(
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
                                fieldInfo.SetValue(
                                    obj,
                                    !flag.Negated);
                            }

                            else if (flag.Value.type == PropertyType.BOOLEAN)
                            {
                                fieldInfo.SetValue(
                                    obj,
                                    flag.Value.value);
                            }

                            else
                                throw new Exception($"Mismatching types ({flag.Value.type} => {fieldInfo.FieldType})");
                        }

                        break;

                    default:

                        throw new NotImplementedException("Invalid type");
                }

            }

            return true;
        }

        private static bool IsIterable(Type fieldType)
        {
            return fieldType.IsArray || (fieldType != typeof(string) && fieldType.IsEnumerator());
        }

        private static Type GetIterableItemType(Type fieldType)
        {
            if (fieldType.IsArray)
                return fieldType.GetElementType();

            if (fieldType.IsEnumerator())
            {
                var enumType = fieldType
                    .GetInterfaces()
                    .Where(
                        t => t.IsGenericType &&
                        t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();

                return enumType ?? fieldType;
            }

            throw new NotImplementedException();
        }

        private static dynamic ConvertEnumerable(Type fieldType, IEnumerable<dynamic> obj)
        {
            Type itemType = GetIterableItemType(fieldType);

            var objEnum = typeof(Enumerable)
                    .GetMethod("Cast")
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new object[] { obj });

            if (fieldType.IsArray)

                return typeof(Enumerable)
                    .GetMethod("ToArray")
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new object[] { objEnum });

            if (fieldType.IsList())

                return typeof(Enumerable)
                    .GetMethod("ToList")
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new object[] { objEnum });

            throw new NotImplementedException();
        }
    }
}
