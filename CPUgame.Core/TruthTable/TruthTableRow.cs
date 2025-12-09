using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUgame.Core.TruthTable
{
    /// <summary>
    /// Represents a single row in the truth table
    /// </summary>
    public class TruthTableRow
    {
        public List<bool> InputValues { get; }
        public List<bool> OutputValues { get; }

        public TruthTableRow(List<bool> inputValues, List<bool> outputValues)
        {
            InputValues = new List<bool>(inputValues);
            OutputValues = new List<bool>(outputValues);
        }
    }
}
