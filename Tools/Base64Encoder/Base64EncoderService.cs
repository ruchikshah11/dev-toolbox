using System.Text;

namespace DevToolbox.Tools.Base64Encoder
{
    public static class Base64EncoderService
    {
        public static string Encode(string input) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(input ?? string.Empty));

        public static string Decode(string input)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String((input ?? string.Empty).Trim()));
            }
            catch (FormatException ex)
            {
                throw new FormatException("Not valid Base64 text.", ex);
            }
        }
    }
}
