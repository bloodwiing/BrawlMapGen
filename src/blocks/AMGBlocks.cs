using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using BMG;
using System;
using AMGBlocks.Action;
using AMGBlocks.Condition;
using AMGBlocks.Value;

namespace AMGBlocks
{

    public class AMGBlocks
    {
        public static AMGState.Version version = new AMGState.Version(1, 1, 0, "Beta");
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

}
