using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace BMG
{
    // ----- VERSION -------------

    public class AMGBlocks
    {
        public static AMGState.Version version = new AMGState.Version(1, 1, 0, "Beta");
    }

    // ----- DATA TYPES -------------

    public class ColorData
    {
        public byte r { get; set; }
        public byte g { get; set; }
        public byte b { get; set; }

        public static byte ByteClamp(int value)
        {
            if (value > 255)
                return (byte)(value % 256);
            if (value < 0)
                return 0;
            return (byte)value;
        }
        public static byte ByteClamp(float value) => ByteClamp((int)MathF.Round(value));

        public static implicit operator ColorData(int value)
        {
            byte newValue = ByteClamp(value);
            return new ColorData()
            {
                r = newValue,
                g = newValue,
                b = newValue
            };
        }

        public override bool Equals(object obj)
        {
            return obj is ColorData data &&
                   r == data.r &&
                   g == data.g &&
                   b == data.b;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(r, g, b);
        }

        public static ColorData operator +(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(a.r + b.r),
                g = ByteClamp(a.g + b.g),
                b = ByteClamp(a.b + b.b)
            };
        }

        public static ColorData operator -(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(a.r - b.r),
                g = ByteClamp(a.g - b.g),
                b = ByteClamp(a.b - b.b)
            };
        }

        public static ColorData operator *(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(a.r * b.r / 255),
                g = ByteClamp(a.g * b.g / 255),
                b = ByteClamp(a.b * b.b / 255)
            };
        }

        public static ColorData operator /(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(b.r * 255 / a.r),
                g = ByteClamp(b.g * 255 / a.g),
                b = ByteClamp(b.b * 255 / a.b)
            };
        }

        public static ColorData operator %(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(b.r * 255 % a.r),
                g = ByteClamp(b.g * 255 % a.g),
                b = ByteClamp(b.b * 255 % a.b)
            };
        }

        public static ColorData Pow(ColorData b, float p)
        {
            return new ColorData()
            {
                r = ByteClamp(MathF.Pow((float)b.r / 255, p)),
                g = ByteClamp(MathF.Pow((float)b.g / 255, p)),
                b = ByteClamp(MathF.Pow((float)b.b / 255, p))
            };
        }
        public ColorData Pow(float p) => Pow(this, p);

        public static bool operator ==(ColorData a, ColorData b)
        {
            return (a.r == b.r) && (a.g == b.g) && (a.b == b.b);
        }

        public static bool operator !=(ColorData a, ColorData b)
        {
            return (a.r != b.r) || (a.g != b.g) || (a.b != b.b);
        }

        public static bool operator >(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b > b.r + b.g + b.b;
        }

        public static bool operator >=(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b >= b.r + b.g + b.b;
        }

        public static bool operator <(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b < b.r + b.g + b.b;
        }

        public static bool operator <=(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b <= b.r + b.g + b.b;
        }

        public override string ToString()
        {
            return string.Format("ColorData<R {0}, G {1}, B {2}>", r, g, b);
        }

        public static bool ObjectHasColorData(object obj)
        {
            return obj is ColorData || (obj is MathBlock math && math.IsColor) || (obj is PARAMETERBlock par && par.ValueType() == BlockData.ValueType.color);
        }
    }

    // ----- INTEFACES -------------

    public interface IBlock
    {
        string type { get; set; }
        void SetParameters(AMGBlockParameters pars);
    }

    public interface IActionBlock : IBlock 
    {
        object Run();
    }
    public interface IConditionBlock : IBlock
    {
        bool Solve();
    }
    public interface IValueBlock : IBlock
    {
        BlockData.ValueType ValueType();

        float Float();
        ColorData Color();
        string String();
        bool Boolean();

        object Data();
    }

    // ----- ABSTRACTS -------------

    public class LogicInput
    {
        public object Data { get; private set; }

        public void SetValue(object value)
        {
            if (value is bool)
                Data = value;
            else if (value is JObject jObject)
                Data = jObject.ToObject(AMGBlockReader.LocateType(jObject));
            else
                throw new ApplicationException("Invalid conditional block input: " + value.ToString());
        }

        public bool GetValue()
        {
            if (Data is IConditionBlock block)
                return block.Solve();
            return (bool)Data;
        }
    }

    public abstract class LogicalCondition : IConditionBlock
    {
        public string type { get; set; }

        public virtual bool Solve()
        {
            throw new NotImplementedException();
        }

        public void SetParameters(AMGBlockParameters pars)
        {
            if (_inputA.Data is IBlock aBlock)
                aBlock.SetParameters(pars);
            if (_inputB.Data is IBlock bBlock)
                bBlock.SetParameters(pars);
        }

        private readonly LogicInput _inputA = new LogicInput();
        public object a
        {
            get => _inputA.Data;
            set => _inputA.SetValue(value);
        }
        public bool GetA() => _inputA.GetValue();

        private readonly LogicInput _inputB = new LogicInput();
        public object b
        {
            get => _inputB.Data;
            set => _inputB.SetValue(value);
        }
        public bool GetB() => _inputB.GetValue();
    }

    public abstract class ValueBlock : IValueBlock
    {
        public string type { get; set; }

        public virtual bool Boolean()
        {
            throw new NotImplementedException();
        }

        public virtual float Float()
        {
            throw new NotImplementedException();
        }

        public virtual ColorData Color()
        {
            throw new NotImplementedException();
        }

        public virtual string String()
        {
            throw new NotImplementedException();
        }

        public virtual object Data()
        {
            throw new NotImplementedException();
        }

        public virtual BlockData.ValueType ValueType()
        {
            throw new NotImplementedException();
        }

        public virtual void SetParameters(AMGBlockParameters pars)
        {
            throw new NotImplementedException();
        }
    }

    public class BlockData
    {
        public object Data { get; private set; }
        public ValueType Type { get; private set; }
        public enum ValueType
        {
            @null = 0,

            integer = 1,
            @int = 1,

            @float = 1,
            flt = 1,

            @string = 2,
            str = 2,

            character = 2,
            @char = 2,

            boolean = 3,
            @bool = 3,

            @object = 4,
            obj = 4,

            color = 5,
            clr = 5
        }

        public BlockData() { }
        public BlockData(object data) => SetData(data);

        public void SetData(object value)
        {
            if (value is ColorData)
            {
                Type = ValueType.color;
                Data = value;
            }
            else if (value is JObject jObject)
            {
                if (jObject.ContainsKey("r") && jObject.ContainsKey("g") && jObject.ContainsKey("b") && !jObject.ContainsKey("type"))
                    jObject.Add("type", "COLOR");

                if ((string)jObject.Property("type") == "COLOR")
                {
                    Type = ValueType.color;
                    Data = jObject.ToObject<ColorData>();
                }
                else
                {
                    Type = ValueType.@object;
                    Data = (ValueBlock)jObject.ToObject(AMGBlockReader.LocateType(jObject));
                }
            }    
            else
            {
                JValue jvalue = new JValue(value);
                switch (jvalue.Type)
                {
                    case JTokenType.Integer:
                        Type = ValueType.integer;
                        Data = Convert.ToSingle(jvalue.Value<int>());
                        break;
                    case JTokenType.Float:
                        Type = ValueType.@float;
                        Data = jvalue.Value<float>();
                        break;
                    case JTokenType.String:
                        Type = ValueType.@string;
                        Data = jvalue.Value<string>();
                        break;
                    case JTokenType.Boolean:
                        Type = ValueType.boolean;
                        Data = jvalue.Value<bool>();
                        break;
                    default:
                        throw new ApplicationException("Invalid value block input: " + value.ToString());
                }
            }
        }

        public float GetNumber()
        {
            if (Type == ValueType.obj)
                return ((ValueBlock)Data).Float();

            if (Type != ValueType.integer)
                throw new Exception("Value is not a number");

            return Convert.ToSingle(Data);
        }

        public ColorData GetColor()
        {
            if (Type == ValueType.obj)
                return ((ValueBlock)Data).Color();

            if (Type == ValueType.integer)
                return Convert.ToInt16(Data);

            if (Type != ValueType.color)
                throw new Exception("Value is not a color");

            return (ColorData)Data;
        }

        public string GetString()
        {
            if (Type == ValueType.obj)
                return ((ValueBlock)Data).String();

            if (Type != ValueType.str)
                return Data.ToString();

            return (string)Data;
        }

        public bool GetBoolean()
        {
            if (Type == ValueType.obj)
                return ((ValueBlock)Data).Boolean();

            if (Type != ValueType.integer)
                throw new Exception("Value is not a boolean");

            return (bool)Data;
        }

        public object GetValue()
        {
            if (Type == ValueType.obj)
                return ((ValueBlock)Data).Data();

            return Data;
        }

        public string GetTypeString()
        {
            return new string[] { "Null", "Integer", "String", "Boolean", "Object", "Color" }[(int)Type];
        }
    }

    public abstract class ComparingCondition : IConditionBlock
    {
        public string type { get; set; }

        public virtual bool Solve()
        {
            throw new NotImplementedException();
        }

        public void SetParameters(AMGBlockParameters pars)
        {
            if (a is IBlock aBlock)
                aBlock.SetParameters(pars);
            if (b is IBlock bBlock)
                bBlock.SetParameters(pars);
        }

        public bool IsColor => ColorData.ObjectHasColorData(a) || ColorData.ObjectHasColorData(b);

        private readonly BlockData _inputA = new BlockData();
        public object a
        {
            get => _inputA.Data;
            set => _inputA.SetData(value);
        }
        public object GetA() => _inputA.GetValue();
        public float GetANumber() => _inputA.GetNumber();
        public ColorData GetAColor() => _inputA.GetColor();

        private readonly BlockData _inputB = new BlockData();
        public object b
        {
            get => _inputB.Data;
            set => _inputB.SetData(value);
        }
        public object GetB() => _inputB.GetValue();
        public float GetBNumber() => _inputB.GetNumber();
        public ColorData GetBColor() => _inputB.GetColor();
    }

    public abstract class MathBlock : ValueBlock
    {
        public override BlockData.ValueType ValueType()
        {
            if (IsColor)
                return BlockData.ValueType.color;
            return BlockData.ValueType.integer;
        }

        public override void SetParameters(AMGBlockParameters pars)
        {
            if (a is IBlock aBlock)
                aBlock.SetParameters(pars);
            if (b is IBlock bBlock)
                bBlock.SetParameters(pars);
        }

        public abstract float Calculate();
        public abstract ColorData CalculateColor();

        public bool IsColor => ColorData.ObjectHasColorData(a) || ColorData.ObjectHasColorData(b);

        public override float Float()
        {
            return Calculate();
        }

        public override ColorData Color()
        {
            if (IsColor)
                return CalculateColor();
            return (ColorData)Calculate();
        }

        public override object Data()
        {
            if (IsColor)
                return CalculateColor();
            return Calculate();
        }

        private BlockData _inputA = new BlockData();
        public object a
        {
            get => _inputA.Data;
            set => _inputA.SetData(value);
        }
        public float GetA() => _inputA.GetNumber();
        public ColorData GetAColor() => _inputA.GetColor();

        private BlockData _inputB = new BlockData();
        public object b
        {
            get => _inputB.Data;
            set => _inputB.SetData(value);
        }
        public float GetB() => _inputB.GetNumber();
        public ColorData GetBColor() => _inputB.GetColor();
    }

    // ----- ACTION BLOCKS -------------

    public class RETURNBlock : IActionBlock
    {
        public string type { get; set; }  // RETURN

        private readonly BlockData _value = new BlockData();
        public object value
        {
            get => _value.Data;
            set => _value.SetData(value);
        }
        public object GetValue() => _value.GetValue();

        public object Run()
        {
            return GetValue();
        }

        public void SetParameters(AMGBlockParameters pars)
        {
            if (value is IBlock block)
                block.SetParameters(pars);
        }
    }

    public class IFBlock : IActionBlock
    {
        public string type { get; set; }  // IF

        public IConditionBlock condition { get; set; }
        public IActionBlock then { get; set; }
        public IActionBlock @else { get; set; }

        public object Run()
        {
            if (condition.Solve()) return then.Run();
            if (@else != null) return @else.Run();
            return null;
        }

        public void SetParameters(AMGBlockParameters pars)
        {
            condition.SetParameters(pars);
            then.SetParameters(pars);
            if (@else != null)
                @else.SetParameters(pars);
        }
    }

    public class RUNBlock : IActionBlock
    {
        public string type { get; set; }  // RUN

        private AMGBlockFunction _function;

        public string name { get; set; }

        private Dictionary<string, object> _parameters;
        public Dictionary<string, object> parameters
        {
            get => _parameters;
            set
            {
                _parameters = new Dictionary<string, object>();

                var keys = new List<string>(value.Keys);

                foreach (var k in keys)
                {
                    var data = new BlockData(value[k]);

                    if (data.Type == BlockData.ValueType.@object)
                        _parameters.Add(k, data);
                    else
                        _parameters.Add(k, value[k]);
                }
            }
        }

        public object Run()
        {
            if (parameters != null)
            {
                Dictionary<string, object> realParams = new Dictionary<string, object>();

                foreach (var pair in parameters)
                {
                    if (pair.Value is BlockData block)
                        realParams.Add(pair.Key, block.GetValue());
                    else
                        realParams.Add(pair.Key, pair.Value);
                }

                return _function.Run(realParams);
            }
            return _function.Run();
        }

        public void SetParameters(AMGBlockParameters pars)
        {
            _function = pars.GetManager().GetFunction(name);

            foreach (var pair in parameters)
            {
                if (pair.Value is BlockData data)
                {
                    if (data.Type == BlockData.ValueType.@object)
                    {
                        ((IValueBlock)data.Data).SetParameters(pars);
                    }
                }
            }
        }
    }

    // ----- CONDITION BLOCKS | COMPARE -------------

    public class EQUBlock : ComparingCondition
    {
        public override bool Solve()
        {
            return GetA().Equals(GetB());
        }
    }

    public class NEQBlock : ComparingCondition
    {
        public override bool Solve()
        {
            return !GetA().Equals(GetB());
        }
    }

    public class LSSBlock : ComparingCondition
    {
        public override bool Solve()
        {
            if (IsColor)
                return GetAColor() < GetBColor();
            return GetANumber() < GetBNumber();
        }
    }

    public class LEQBlock : ComparingCondition
    {
        public override bool Solve()
        {
            if (IsColor)
                return GetAColor() <= GetBColor();
            return GetANumber() <= GetBNumber();
        }
    }

    public class GTRBlock : ComparingCondition
    {
        public override bool Solve()
        {
            if (IsColor)
                return GetAColor() > GetBColor();
            return GetANumber() > GetBNumber();
        }
    }

    public class GEQBlock : ComparingCondition
    {
        public override bool Solve()
        {
            if (IsColor)
                return GetAColor() >= GetBColor();
            return GetANumber() >= GetBNumber();
        }
    }

    // ----- CONDITION BLOCKS | LOGIC -------------

    public class ORBlock : LogicalCondition
    {
        public override bool Solve()
        {
            return GetA() || GetB();
        }
    }

    public class NORBlock : LogicalCondition
    {
        public override bool Solve()
        {
            return !GetA() && !GetB();
        }
    }

    public class ANDBlock : LogicalCondition
    {
        public override bool Solve()
        {
            return GetA() && GetB();
        }
    }

    public class NANDBlock : LogicalCondition
    {
        public override bool Solve()
        {
            return !GetA() || !GetB();
        }
    }

    public class XORBlock : LogicalCondition
    {
        public override bool Solve()
        {
            return GetA() ^ GetB();
        }
    }

    public class XNORBlock : LogicalCondition
    {
        public override bool Solve()
        {
            return GetA() == GetB();
        }
    }

    public class NOTBlock : IConditionBlock
    {
        public string type { get; set; }  // NOT

        public void SetParameters(AMGBlockParameters pars)
        {
            if (input is IBlock iBlock)
                iBlock.SetParameters(pars);
        }

        private readonly LogicInput _input = new LogicInput();
        public object input
        {
            get => _input.Data;
            set => _input.SetValue(value);
        }

        public bool Solve()
        {
            return !_input.GetValue();
        }
    }

    // ----- VALUE BLOCKS -------------

    public class NUMBERBlock : ValueBlock
    {
        public string key { get; set; }

        public override float Float() => AMGState.GetNumber(key);
        public override ColorData Color() => (ColorData)AMGState.GetNumber(key);
        public override object Data() => AMGState.GetNumber(key);

        public override void SetParameters(AMGBlockParameters pars)
        {
            return;
        }

        public override string String() => AMGState.GetNumber(key).ToString();

        public override BlockData.ValueType ValueType() => BlockData.ValueType.@float;
    }

    public class COLORBlock : ValueBlock
    {
        private readonly ColorData _color = new ColorData();

        public override ColorData Color() => _color;
        public override object Data() => _color;

        public override void SetParameters(AMGBlockParameters pars)
        {
            return;
        }

        public override string String() => _color.ToString();

        public byte r { get => _color.r; set => _color.r = value; }
        public byte g { get => _color.g; set => _color.g = value; }
        public byte b { get => _color.b; set => _color.b = value; }

        public override BlockData.ValueType ValueType() => BlockData.ValueType.color;
    }

    public class PARAMETERBlock : ValueBlock
    {
        public string name { get; set; }

        public override void SetParameters(AMGBlockParameters pars)
        {
            _data = pars.GetData(name);
        }

        public override float Float() => _data.GetNumber();
        public override bool Boolean() => _data.GetBoolean();
        public override string String() => _data.GetString();
        public override ColorData Color() => _data.GetColor();

        public override object Data() => _data.Data;

        private BlockData _data;
        public override BlockData.ValueType ValueType() => _data.Type;
    }

    public class ADDBlock : MathBlock
    {
        public override float Calculate()
        {
            return GetA() + GetB();
        }

        public override ColorData CalculateColor()
        {
            return GetAColor() + GetBColor();
        }
    }

    public class SUBBlock : MathBlock
    {
        public override float Calculate()
        {
            return GetA() - GetB();
        }

        public override ColorData CalculateColor()
        {
            return GetAColor() - GetBColor();
        }
    }

    public class MULBlock : MathBlock
    {
        public override float Calculate()
        {
            return GetA() * GetB();
        }

        public override ColorData CalculateColor()
        {
            return GetAColor() * GetBColor();
        }
    }

    public class DIVBlock : MathBlock
    {
        public override float Calculate()
        {
            return GetA() / GetB();
        }

        public override ColorData CalculateColor()
        {
            return GetAColor() / GetBColor();
        }
    }

    public class DIVFLOORBlock : MathBlock
    {
        public override float Calculate()
        {
            return MathF.Floor(GetA() / GetB());
        }

        public override ColorData CalculateColor()
        {
            return GetAColor() / GetBColor();
        }
    }

    public class REMBlock : MathBlock
    {
        public override float Calculate()
        {
            return GetA() % GetB();
        }

        public override ColorData CalculateColor()
        {
            return GetAColor() % GetBColor();
        }
    }

    public class POWBlock : ValueBlock
    {
        public override BlockData.ValueType ValueType()
        {
            if (IsColor)
                return BlockData.ValueType.color;
            return BlockData.ValueType.integer;
        }

        public override void SetParameters(AMGBlockParameters pars)
        {
            if (@base is IBlock aBlock)
                aBlock.SetParameters(pars);
            if (exponent is IBlock bBlock)
                bBlock.SetParameters(pars);
        }

        public bool IsColor => ColorData.ObjectHasColorData(@base);

        public override float Float()
        {
            return Calculate();
        }

        public override ColorData Color()
        {
            if (IsColor)
                return CalculateColor();
            return (ColorData)Calculate();
        }

        public override object Data()
        {
            if (IsColor)
                return CalculateColor();
            return Calculate();
        }

        private BlockData _base = new BlockData();
        public object @base
        {
            get => _base.Data;
            set => _base.SetData(value);
        }
        public float GetBase() => _base.GetNumber();
        public ColorData GetBaseColor() => _base.GetColor();

        private BlockData _exp = new BlockData();
        public object exponent
        {
            get => _exp.Data;
            set => _exp.SetData(value);
        }
        public float GetExponent() => _exp.GetNumber();

        public float Calculate()
        {
            return MathF.Pow(GetBase(), GetExponent());
        }

        public ColorData CalculateColor()
        {
            return GetBaseColor().Pow(GetExponent());
        }
    }

    public class SQRTBlock : ValueBlock
    {
        public override BlockData.ValueType ValueType()
        {
            if (IsColor)
                return BlockData.ValueType.color;
            return BlockData.ValueType.integer;
        }

        public override void SetParameters(AMGBlockParameters pars)
        {
            if (input is IBlock aBlock)
                aBlock.SetParameters(pars);
        }

        public bool IsColor => ColorData.ObjectHasColorData(input);

        public override float Float()
        {
            return Calculate();
        }

        public override ColorData Color()
        {
            if (IsColor)
                return CalculateColor();
            return (ColorData)Calculate();
        }

        public override object Data()
        {
            if (IsColor)
                return CalculateColor();
            return Calculate();
        }

        private BlockData _input = new BlockData();
        public object input
        {
            get => _input.Data;
            set => _input.SetData(value);
        }
        public float GetInput() => _input.GetNumber();
        public ColorData GetInputColor() => _input.GetColor();

        public float Calculate()
        {
            return MathF.Sqrt(GetInput());
        }

        public ColorData CalculateColor()
        {
            return GetInputColor().Pow(.5f);
        }
    }

    // ----- READER -------------

    public class AMGBlockReader : Newtonsoft.Json.Converters.CustomCreationConverter<IBlock>
    {
        public override IBlock Create(Type objectType)
        {
            throw new NotImplementedException();
        }

        public static Type LocateType(JObject jObject)
        {
            var type = (string)jObject.Property("type");

            switch (type)
            {
                // Action
                case "RETURN":
                    return typeof(RETURNBlock);
                case "IF":
                    return typeof(IFBlock);
                case "RUN":
                    return typeof(RUNBlock);

                // Condition / Compare
                case "==":
                    return typeof(EQUBlock);
                case "!=":
                    return typeof(NEQBlock);
                case "<":
                    return typeof(LSSBlock);
                case "<=":
                    return typeof(LEQBlock);
                case ">":
                    return typeof(GTRBlock);
                case ">=":
                    return typeof(GEQBlock);

                // Condition / Logic
                case "OR":
                    return typeof(ORBlock);
                case "NOR":
                    return typeof(NORBlock);
                case "AND":
                    return typeof(ANDBlock);
                case "NAND":
                    return typeof(NANDBlock);
                case "XOR":
                    return typeof(XORBlock);
                case "XNOR":
                    return typeof(XNORBlock);
                case "NOT":
                    return typeof(NOTBlock);

                // Value
                case "NUMBER":
                    return typeof(NUMBERBlock);
                case "COLOR":
                    return typeof(COLORBlock);
                case "PARAMETER":
                    return typeof(PARAMETERBlock);
                case "+":
                    return typeof(ADDBlock);
                case "-":
                    return typeof(SUBBlock);
                case "*":
                    return typeof(MULBlock);
                case "/":
                    return typeof(DIVBlock);
                case "//":
                    return typeof(DIVFLOORBlock);
                case "%":
                    return typeof(REMBlock);
                case "POW":
                    return typeof(POWBlock);
                case "SQRT":
                    return typeof(SQRTBlock);
            }

            throw new ApplicationException("Unknown block type detected: " + type);
        }

        public IBlock Create(JObject jObject)
        {
            Type type = LocateType(jObject);
            return (IBlock)Activator.CreateInstance(type);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject jObject = JObject.Load(reader);

            var instance = Create(jObject);
            serializer.Populate(jObject.CreateReader(), instance);

            return instance;
        }

    }

    // ----- PARAMETERS -------------

    public class AMGBlockParameters
    {
        private AMGBlockManager _manager;

        private Tiledata.AMGBlocksParameter[] _format;
        private Dictionary<string, BlockData> _data = new Dictionary<string, BlockData>();

        public AMGBlockParameters(Tiledata.AMGBlocksParameter[] format)
        {
            List<string> uniques = new List<string>();

            foreach (var f in format)
                if (uniques.Contains(f.name))
                    throw new ApplicationException("Parameter name " + f.name + " reused");
                else
                    uniques.Add(f.name);

            _format = format;
        }

        public void SetManager(AMGBlockManager manager) => _manager = manager;
        public AMGBlockManager GetManager() => _manager;

        public void SetData(Dictionary<string, object> pars)
        {
            Dictionary<string, BlockData> newPars = new Dictionary<string, BlockData>();

            foreach (var param in _format)
            {
                try
                {
                    var newData = new BlockData();
                    newData.SetData(pars[param.name]);

                    if ((param.type == "Integer" && newData.Type != BlockData.ValueType.integer) ||
                        (param.type == "String" && newData.Type != BlockData.ValueType.@string) ||
                        (param.type == "Boolean" && newData.Type != BlockData.ValueType.boolean) ||
                        (param.type == "Color" && newData.Type != BlockData.ValueType.color))
                        throw new ApplicationException(
                            string.Format(
                                "Parameter {0} type does not match. Expected {1}, got {2}",
                                param.name,
                                param.type,
                                newData.GetTypeString()
                            ));

                    newPars.Add(param.name, newData);
                }
                catch (KeyNotFoundException)
                {
                    throw new ApplicationException("Missing parameter " + param.name);
                }
            }

            _data = newPars;
        }

        public void ClearData()
        {
            _data.Clear();
        }

        public BlockData GetData(string key)
        {
            foreach (var f in _format)
            {
                if (f.name == key)
                {
                    bool exists = _data.TryGetValue(key, out BlockData datum);

                    BlockData.ValueType expected;
                    
                    switch (f.type)
                    {
                        case "Integer":
                            expected = BlockData.ValueType.integer;
                            break;

                        case "String":
                            expected = BlockData.ValueType.@string;
                            break;

                        case "Boolean":
                            expected = BlockData.ValueType.boolean;
                            break;

                        case "Color":
                            expected = BlockData.ValueType.color;
                            break;

                        default:
                            throw new ApplicationException(string.Format(
                                "Parameter '{0}' type is invalid: {1}",
                                key,
                                f.type
                            ));
                    }

                    if (exists)
                    {
                        if (datum.Type != expected)
                            throw new ApplicationException(string.Format(
                                "Parameter '{0}' type mismatch. Expected {1}, Got {2}",
                                key,
                                f.type,
                                datum.GetTypeString()
                            ));
                        return datum;
                    }
                    if (f.@default != null)
                    {
                        var gen = new BlockData(f.@default);
                        if (gen.Type != expected)
                            throw new ApplicationException(string.Format(
                                "Parameter '{0}' type mismatch. Expected {1}, Got {2}",
                                key,
                                f.type,
                                gen.GetTypeString()
                            ));
                        return gen;
                    };
                    
                    switch (expected)
                    {
                        case BlockData.ValueType.integer:
                            return new BlockData(0);
                        case BlockData.ValueType.@string:
                            return new BlockData("");
                        case BlockData.ValueType.boolean:
                            return new BlockData(false);
                        case BlockData.ValueType.color:
                            return new BlockData(new ColorData() { r = 0, g = 0, b = 0 });
                    }
                }
            }
            throw new ApplicationException(string.Format(
                "Parameter key '{0}' is not registered",
                key
            ));
        }
    }

    public class AMGBlockFunction
    {
        private IActionBlock parentBlock;
        private AMGBlockParameters parameters;

        private AMGBlockManager manager;

        public AMGBlockFunction(IActionBlock parentBlock, Tiledata.AMGBlocksParameter[] pars)
        {
            this.parentBlock = parentBlock;
            parameters = new AMGBlockParameters(pars);
        }

        public void RegisterManager(AMGBlockManager manager)
        {
            this.manager = manager;
            parameters.SetManager(manager);
        }
        public AMGBlockManager GetManager() => manager;

        public object Run()
        {
            parameters.ClearData();
            parentBlock.SetParameters(parameters);
            return parentBlock.Run();
        }

        public object Run(Dictionary<string, object> pars)
        {
            parameters.SetData(pars);
            parentBlock.SetParameters(parameters);
            return parentBlock.Run();
        }
    }

    public class AMGBlockManager
    {
        private Dictionary<string, AMGBlockFunction> _funcs = new Dictionary<string, AMGBlockFunction>();

        public void RegisterFunction(string name, AMGBlockFunction function)
        {
            if (!_funcs.TryAdd(name, function))
                throw new ApplicationException("Block function name '" + name + "' already exists");
            function.RegisterManager(this);
        }
        public void RegisterFunction(string name, IActionBlock parentBlock, Tiledata.AMGBlocksParameter[] pars)
        {
            RegisterFunction(name, new AMGBlockFunction(parentBlock, pars));
        }

        public object RunFunction(string name, Dictionary<string, object> parameters)
        {
            if (!_funcs.TryGetValue(name, out var func))
                throw new ApplicationException("Block function with name '" + name + "' does not exist");

            return func.Run(parameters);
        }
        public object RunFunction(string name)
        {
            if (!_funcs.TryGetValue(name, out var func))
                throw new ApplicationException("Block function with name '" + name + "' does not exist");

            return func.Run();
        }

        public AMGBlockFunction GetFunction(string name)
        {
            if (!_funcs.TryGetValue(name, out var func))
                throw new ApplicationException("Block function with name '" + name + "' does not exist");

            return func;
        }
    }
}
