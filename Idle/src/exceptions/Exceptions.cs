using Idle.Extensions;
using Idle.Serialization.Abstract;
using System;

namespace Idle.Exceptions
{
    class ColorSerializationException : Exception
    {
        public ColorSerializationException(Property property, Type receivedType)
            : base($"COLOR '{property.Label}' can only be deserialized into fields of struct Color, not {receivedType}")
        { }
    }

    class MismatchingTypeException : Exception
    {
        public MismatchingTypeException(Property property, Type receivedType)
            : base($"Mismatching types for '{property.Label}' ({property.DisplayType()} => {receivedType})")
        { }
    }

    class MismatchingFlagTypeException : Exception
    {
        public MismatchingFlagTypeException(Property property, Flag flag, Type receivedType)
            : base($"Mismatching types for '{property.Label}:{flag.Name}' ({flag.Value.type} => {receivedType})")
        { }
    }

    class MismatchingDataSizeException : Exception
    {
        public MismatchingDataSizeException(Property property, Flag flag, int receivedSize)
            : base($"Invalid data size for '{property.Label}:{flag.Name}' (Expected 1 : Got {receivedSize})")
        { }
    }

    class ChildFlagArrayException : Exception
    {
        public ChildFlagArrayException(IdleChildFlagBase childFlagAttribute)
            : base($"Cannot differentiate Child Flags when in Property Arrays ('{childFlagAttribute.label}')")
        { }
    }

    class FlagIterableException : Exception
    {
        public FlagIterableException(Type receivedType)
            : base($"Flags ('{receivedType}') cannot be iterable")
        { }
    }

    class FlagCustomClassException : Exception
    {
        public FlagCustomClassException(Type receivedType)
            : base($"Flags ('{receivedType}') cannot be a custom class")
        { }
    }

    class EnumTypeException : Exception
    {
        public EnumTypeException(ContainerInfo containerInfo)
            : base($"Enums ('{containerInfo.Name}') must be NUMBER or TEXT as input")
        { }
    }

    class ShortFlagTypeException : Exception
    {
        public ShortFlagTypeException()
            : base("Short Flags can only be string")
        { }
    }

    class PropertyLabelTypeMismatchException : Exception
    {
        public PropertyLabelTypeMismatchException(TokenType receivedType)
            : base($"Property labels must be TEXT, not {receivedType}")
        { }
    }

    class FlagValueTypeException : Exception
    {
        public FlagValueTypeException(TokenType receivedType)
            : base($"Value Flags must be TEXT_ROW, TEXT, NUMBER or FRACTION. Received {receivedType}")
        { }
    }

    class MissingPropertyValueException : Exception
    {
        public MissingPropertyValueException(Property property)
            : base($"Property '{property.Label}' must have a Value")
        { }
    }

    class PropertyListTypeMismatchException : Exception
    {
        public PropertyListTypeMismatchException(Property property, Item newItem)
            : base($"Property List Type mismatch on '{property.Label}' ({property.DataType} != {newItem.ValueType})")
        { }
    }

    class FlagNameException : Exception
    {
        public FlagNameException()
            : base("Flag Name must be TEXT")
        { }
    }

    class ColorParseException : Exception
    {
        public ColorParseException()
            : base("Invalid Color parsing data")
        { }
    }
}
