using BMG;
using Newtonsoft.Json.Linq;
using System;

namespace AMGBlocks.Value
{

    // ----- ABSTRACTS -------------

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
}