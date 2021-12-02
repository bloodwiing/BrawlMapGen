using AMGBlocks.Action;
using AMGBlocks.Condition;
using AMGBlocks.Value;
using BMG;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AMGBlocks
{

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


    public class AMGBlockParameters
    {
        private AMGBlockManager _manager;

        private BlocksParameterBase[] _format;
        private Dictionary<string, BlockData> _data = new Dictionary<string, BlockData>();

        public AMGBlockParameters(BlocksParameterBase[] format)
        {
            List<string> uniques = new List<string>();

            foreach (var f in format)
                if (uniques.Contains(f.Name))
                    throw new ApplicationException($"Parameter NAME \"{f.Name}\" reused");
                else
                    uniques.Add(f.Name);

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
                    newData.SetData(pars[param.Name]);

                    if ((param.Type == "Integer" && newData.Type != BlockData.ValueType.integer) ||
                        (param.Type == "String" && newData.Type != BlockData.ValueType.@string) ||
                        (param.Type == "Boolean" && newData.Type != BlockData.ValueType.boolean) ||
                        (param.Type == "Color" && newData.Type != BlockData.ValueType.color))
                        throw new ApplicationException(
                            string.Format(
                                "Parameter {0} type does not match. Expected {1}, got {2}",
                                param.Name,
                                param.Type,
                                newData.GetTypeString()
                            ));

                    newPars.Add(param.Name, newData);
                }
                catch (KeyNotFoundException)
                {
                    throw new ApplicationException("Missing parameter " + param.Name);
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
                if (f.Name == key)
                {
                    bool exists = _data.TryGetValue(key, out BlockData datum);

                    BlockData.ValueType expected;

                    switch (f.Type)
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
                                f.Type
                            ));
                    }

                    if (exists)
                    {
                        if (datum.Type != expected)
                            throw new ApplicationException(string.Format(
                                "Parameter '{0}' type mismatch. Expected {1}, Got {2}",
                                key,
                                f.Type,
                                datum.GetTypeString()
                            ));
                        return datum;
                    }
                    if (f.Default != null)
                    {
                        var gen = new BlockData(f.Default);
                        if (gen.Type != expected)
                            throw new ApplicationException(string.Format(
                                "Parameter '{0}' type mismatch. Expected {1}, Got {2}",
                                key,
                                f.Type,
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

        public AMGBlockFunction(IActionBlock parentBlock, BlocksParameterBase[] pars)
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
        public void RegisterFunction(string name, IActionBlock parentBlock, BlocksParameterBase[] pars)
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