using AMGBlocks.Value;
using Newtonsoft.Json.Linq;
using System;

namespace AMGBlocks.Condition
{

    // ----- BASES -------------

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
}