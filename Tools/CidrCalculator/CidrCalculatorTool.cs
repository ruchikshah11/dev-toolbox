using DevToolbox.Core;
using DevToolbox.UI;

namespace DevToolbox.Tools.CidrCalculator
{
    public class CidrCalculatorTool : ITool
    {
        public string Category => "Converters";
        public string Name => "IP/CIDR Subnet Calculator";
        public string Description => "Computes a subnet's network/broadcast address, subnet and wildcard masks, and usable host range from an IPv4 address and CIDR prefix.";

        /// <summary>Wires the CIDR calculation into the shared paste-in/run/see-result shell.</summary>
        public Control CreateView() => new TextTransformControl(
            "Enter an IPv4 address with a CIDR prefix",
            "Result",
            new[]
            {
                new TextTransformAction("Calculate", input => CidrCalculatorService.FormatSummary(CidrCalculatorService.Calculate(input)), Primary: true)
            },
            "192.168.1.10/24");
    }
}
