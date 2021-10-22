using AMGBlocks.Value;
using System.Collections.Generic;

namespace AMGBlocks.Action
{

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
                if (pair.Value is BlockData data && data.Type == BlockData.ValueType.@object)
                    ((IValueBlock)data.Data).SetParameters(pars);
            }
        }
    }
}