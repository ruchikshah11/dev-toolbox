using DevToolbox.Core;

namespace DevToolbox.Tools.CreditCardTool
{
    public class CreditCardTool : ITool
    {
        public string Category => "Validators";
        public string Name => "Credit Card Number Generator & Validator";
        public string Description => "Generates and validates test credit card numbers using the Luhn algorithm.";

        public Control CreateView() => new CreditCardControl();
    }
}
