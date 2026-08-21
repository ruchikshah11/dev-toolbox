namespace DevToolbox.Tools.ClaimsIdentity
{
    public readonly record struct ClaimTypeInfo(string DisplayName, string Prefix, bool HasValue, string ValueHint)
    {
        public override string ToString() => DisplayName;
    }

    public readonly record struct ClaimsDecodeResult(string ClaimType, string Prefix, string Value, string Raw);

    /// <summary>
    /// Best-effort reference implementation of SharePoint's claims-encoded identity string
    /// format (i:0#.f|membership|..., c:0t.c|tenant|..., etc.). Covers the prefixes commonly
    /// documented across on-prem SharePoint and SharePoint Online - not exhaustive, and a
    /// custom trusted identity provider uses its own provider name instead of the "adfs"
    /// example shown here.
    /// </summary>
    public static class ClaimsIdentityService
    {
        public static readonly ClaimTypeInfo[] ClaimTypes =
        {
            new("Windows User", "i:0#.w|", true, "DOMAIN\\username"),
            new("Forms-Based Membership User", "i:0#.f|membership|", true, "username or email"),
            new("Forms-Based Role", "c:0-.f|rolemanager|", true, "role name"),
            new("Trusted Provider (e.g. ADFS) User", "i:0#.t|adfs|", true, "user identifier (UPN or NameID)"),
            new("Azure AD Security Group", "c:0t.c|tenant|", true, "Azure AD group object ID"),
            new("Everyone", "c:0(.s|true", false, ""),
            new("All Users (Windows)", "c:0!.s|windows", false, ""),
        };

        public static ClaimsDecodeResult Decode(string claim)
        {
            claim = (claim ?? string.Empty).Trim();
            if (claim.Length == 0) throw new FormatException("Paste a claims-encoded identity string to decode.");

            var claimType = ClaimTypes
                .Where(c => claim.StartsWith(c.Prefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.Prefix.Length)
                .Select(c => (ClaimTypeInfo?)c)
                .FirstOrDefault();

            if (claimType is null)
            {
                return new ClaimsDecodeResult("Unrecognized claim prefix", string.Empty, claim, claim);
            }

            var value = claim.Substring(claimType.Value.Prefix.Length);
            return new ClaimsDecodeResult(claimType.Value.DisplayName, claimType.Value.Prefix.TrimEnd('|'), value, claim);
        }

        public static string Encode(ClaimTypeInfo claimType, string value)
        {
            if (!claimType.HasValue) return claimType.Prefix;

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException($"Enter a value ({claimType.ValueHint}).");
            }
            return claimType.Prefix + value;
        }
    }
}
