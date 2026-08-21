namespace DevToolbox.Core
{
    // A richer description + bullet list for the "Documentation" page, keyed by the tool's
    // exact ITool.Name. DisplayName overrides the heading shown on the docs page only (e.g.
    // freeformatter.com's own tool-list page titles its formatters "X / Beautifier", even
    // though its regular nav just says "X Formatter" - same split is kept here).
    public sealed record ToolDoc(string Description, string[] Bullets, string? DisplayName = null);

    /// <summary>
    /// Content for the "Documentation" page, styled after freeformatter.com's own per-category
    /// tool-list pages (formatters.html, validators.html, converters.html, etc.) - paragraph
    /// description plus a bullet list of what the tool does. Wording is adapted from those
    /// pages rather than copied verbatim wherever the real site's copy describes a capability
    /// this app doesn't actually have (e.g. their Message Digester lists a dozen hash
    /// algorithms; ours supports MD5/SHA-256/SHA-512) - the goal is the same tone and level of
    /// detail, describing what THIS app's tool actually does.
    /// </summary>
    public static class ToolHighlights
    {
        public static ToolDoc? For(string toolName) => Data.TryGetValue(toolName, out var doc) ? doc : null;

        private static readonly Dictionary<string, ToolDoc> Data = new()
        {
            ["JSON Formatter"] = new ToolDoc(
                "Formats a JSON string/file with your desired indentation level, creating an object tree with color highlights. You can clearly identify object constructs (objects, arrays and members). The JSON tree that is created can be navigated by collapsing the individual nodes one at a time if desired.",
                new[]
                {
                    "Formats your JSON string/file with a choice of 6 indentation levels: 2 spaces, 3 spaces, 4 spaces, compact mode, JavaScript escaped and tab separated",
                    "Creates a tree representation of the JSON objects for easy navigation",
                    "Color highlights the different constructs of your JSON objects",
                    "Preserves \"//\" and \"/* */\" comments (JSONC) through the indented styles",
                    "Supports copy-paste or file upload, and a pop-out result window",
                },
                DisplayName: "JSON Formatter / Beautifier"),

            ["HTML Formatter"] = new ToolDoc(
                "Formats an HTML string/file with your desired indentation level. The formatting rules aren't configurable beyond indentation, but it keeps single-text-node elements inline for readability.",
                new[]
                {
                    "Formats the HTML with 4 indentation levels: 2 spaces, 3 spaces, 4 spaces and tab separated",
                    "Keeps simple elements like <p>Hello</p> inline instead of over-expanding them",
                    "Supports copy-paste or file upload",
                },
                DisplayName: "HTML Formatter / Beautifier"),

            ["XML Formatter"] = new ToolDoc(
                "Formats an XML string/file with your desired indentation level. The formatting rules aren't configurable, but it uses a per-element indentation pattern giving the best readability, and carries any XML comments through unchanged.",
                new[]
                {
                    "Formats the XML with 4 indentation levels: 2 spaces, 3 spaces, 4 spaces and tab separated",
                    "Preserves <!-- comments --> in their original position",
                    "Supports copy-paste or file upload",
                },
                DisplayName: "XML Formatter / Beautifier"),

            ["SQL Formatter"] = new ToolDoc(
                "Formats any SQL query with your desired indentation level, even if your SQL statement doesn't fully parse. You can also change the case of recognized SQL keywords. Built to be database-agnostic rather than tied to one SQL dialect.",
                new[]
                {
                    "Formats the SQL with a chosen indentation, reflowing major clauses (SELECT/FROM/WHERE/JOIN/GROUP BY/ORDER BY/...) onto their own lines",
                    "Formats regardless of the statement's validity - it's a heuristic beautifier, not a full parser",
                    "Uppercases recognized keywords without touching string literal contents",
                    "Supports SELECT, INSERT, UPDATE and DELETE statements",
                    "Supports copy-paste or file upload",
                },
                DisplayName: "SQL Formatter / Beautifier"),

            ["JSON Validator"] = new ToolDoc(
                "Validates that a JSON string/file parses correctly and reports the line and position of any syntax error.",
                new[]
                {
                    "Validates your JSON string/file and reports line/position of the first error",
                    "Accepts JSONC - \"//\" and \"/* */\" comments and trailing commas are tolerated",
                    "Shows the comment-stripped, re-serialized document alongside the result",
                    "Supports copy-paste or file upload",
                }),

            ["HTML Validator"] = new ToolDoc(
                "Validates the HTML string/file for structural well-formedness using a lenient, real-world-tolerant parser rather than strict W3C/doctype-based validation.",
                new[]
                {
                    "Reports unclosed tags, mismatches and other structural issues the parser noticed",
                    "Not a strict W3C validator - won't catch every issue a browser or validator.w3.org would flag",
                    "Supports copy-paste or file upload",
                }),

            ["XML Validator"] = new ToolDoc(
                "Validates the XML string/file against an XSD string/file you provide. XSD files are 'XML Schemas' that describe the structure of an XML document. The validator checks for well-formedness first, then validates against the schema if one is given.",
                new[]
                {
                    "Checks well-formedness even without a schema",
                    "Validates against a provided XSD and reports every schema violation with line/position",
                    "Leave the XSD box blank to only check well-formedness",
                }),

            ["XPath Tester"] = new ToolDoc(
                "Executes an XPath query against an XML document and outputs the matched content, one match per line.",
                new[]
                {
                    "XPath 1.0 (System.Xml.XPath) compatible",
                    "Reports the number of matches and each match's value",
                    "Handles both node-set results and scalar (boolean/number/string) expressions",
                }),

            ["Credit Card Number Generator & Validator"] = new ToolDoc(
                "Validates credit card numbers and also generates fake credit card numbers for testing. These numbers are for testing purposes only and will not work with a real payment processor.",
                new[]
                {
                    "Validates a card number's checksum using the Luhn (Mod 10) algorithm",
                    "Detects the card brand from its number (Visa, Mastercard, Amex, Discover)",
                    "Generates test-only numbers for a chosen brand with the correct prefix and length",
                }),

            ["Regular Expression Tester"] = new ToolDoc(
                "Tests a regular expression against sample input and highlights every match, so you know exactly where a match occurs. Runs against the .NET regular expression engine.",
                new[]
                {
                    "Supports IgnoreCase, Multiline, Singleline (DOTALL) and IgnorePatternWhitespace flags",
                    "Lists every match's index, length and value",
                    "Shows named and numbered capture group values for each match",
                }),

            ["Java Regular Expression Tester"] = new ToolDoc(
                "Tests a regular expression against sample input, built as a close approximation of java.util.regex using .NET's regex engine (there's no Java runtime available to run against directly).",
                new[]
                {
                    "Highlights all matches and shows group details, same as the .NET Regular Expression Tester",
                    "Close to java.util.regex for most patterns - Java-only features like possessive quantifiers aren't supported",
                }),

            ["Cron Expression Generator (Quartz)"] = new ToolDoc(
                "Parses a 6-field Quartz-style cron expression and shows when it will next run.",
                new[]
                {
                    "Supports *, single values, ranges (a-b), steps (*/n), comma lists, and ? for day fields",
                    "Computes and lists the next 5 fire times from now",
                    "Quartz extensions like L/W/# aren't supported",
                }),

            ["XSD Generator"] = new ToolDoc(
                "Generates an XSD (XML Schema) from a sample XML file. Simply paste or upload your XML document and let the generator infer the structure.",
                new[]
                {
                    "Infers element/attribute structure and data types from the sample XML using .NET's schema inference engine",
                    "Supports copy-paste or file upload",
                }),

            ["XSL Transformer"] = new ToolDoc(
                "Transforms an XML file using an XSL stylesheet (XSL Transformation).",
                new[]
                {
                    "Takes both the XML document and the XSLT stylesheet as input",
                    "Shows the transformed output, or a clear error if either input is invalid",
                },
                DisplayName: "XSL Transformer (XSLT)"),

            ["XML to JSON Converter"] = new ToolDoc(
                "Converts an XML file into JSON.",
                new[]
                {
                    "Converts the XML document tree directly into an equivalent JSON structure",
                    "Supports copy-paste or file upload",
                }),

            ["JSON to XML Converter"] = new ToolDoc(
                "Converts a JSON file into XML.",
                new[]
                {
                    "Converts the JSON document tree directly into an equivalent XML structure",
                    "JSON with a single root object converts directly; arrays or multi-property JSON may need a wrapper object",
                }),

            ["CSV to XML Converter"] = new ToolDoc(
                "Converts CSV data into XML, one <Row> element per record.",
                new[]
                {
                    "Handles quoted fields with embedded commas, quotes and newlines (RFC 4180)",
                    "Sanitizes column names into valid XML element names automatically",
                }),

            ["CSV to JSON Converter"] = new ToolDoc(
                "Converts CSV data into a JSON array of objects, one per record.",
                new[]
                {
                    "Handles quoted fields with embedded commas, quotes and newlines (RFC 4180)",
                    "Uses the first row as the property names for every object",
                }),

            ["YAML to JSON Converter"] = new ToolDoc(
                "Converts a YAML file into JSON.",
                new[]
                {
                    "Converts nested mappings, sequences and scalars into the equivalent indented JSON",
                }),

            ["JSON to YAML Converter"] = new ToolDoc(
                "Converts a JSON file into YAML.",
                new[]
                {
                    "Converts objects, arrays and values into equivalent YAML mappings and sequences",
                }),

            ["Epoch Timestamp To Date"] = new ToolDoc(
                "Converts an epoch/Unix timestamp into a human-readable date, and does the inverse too - converts a human-readable date into an epoch/Unix timestamp. Starts prefilled with the current timestamp.",
                new[]
                {
                    "Auto-detects seconds vs. milliseconds based on the timestamp's magnitude",
                    "Converts epoch timestamps to both UTC and local date/time",
                    "Converts a date/time string back into Unix seconds and milliseconds",
                }),

            ["Number Base Converter"] = new ToolDoc(
                "Converts a number between binary, octal, decimal, and hexadecimal, all shown at once.",
                new[]
                {
                    "Accepts an optional 0x/0b/0o prefix regardless of which base you selected as the source",
                    "Supports negative numbers",
                    "Updates live as you type or change the source base",
                }),

            ["Timezone Converter"] = new ToolDoc(
                "Converts one date/time across every timezone Windows knows about (~140), shown as a searchable, live-updating list.",
                new[]
                {
                    "Pick a date/time (or click Now) and the source zone it's meant to represent",
                    "DST transitions are handled automatically via .NET's TimeZoneInfo, using each zone's own rules for the date you picked",
                    "Live search filters the zone list as you type, same as the other reference-table tools",
                }),

            ["IP/CIDR Subnet Calculator"] = new ToolDoc(
                "Computes a subnet's key addresses and usable host range from an IPv4 address and CIDR prefix (e.g. 192.168.1.10/24).",
                new[]
                {
                    "Shows network address, broadcast address, subnet mask, and wildcard mask",
                    "Shows the first/last usable host and total/usable address counts",
                    "Correctly handles /31 (point-to-point) and /32 (single host), which have no separate network/broadcast address to exclude",
                }),

            ["PDF Password Remover"] = new ToolDoc(
                "Removes password protection from an encrypted PDF and saves an unlocked copy, given the PDF's own password.",
                new[]
                {
                    "Supports RC4, AES-128, and AES-256(-ish, revision 5/6) encrypted PDFs",
                    "Accepts either the user (open) password or the owner (permissions) password",
                }),

            ["Word to PDF"] = new ToolDoc(
                "Converts a .docx to PDF - a text/formatting-based conversion, not a pixel-perfect layout engine.",
                new[]
                {
                    "Renders headings, paragraph text, and bold/italic emphasis, with automatic word-wrapping and page breaks",
                    "Tables, images, and multi-column layouts are not preserved",
                    "Optionally protects the output PDF with a password (shares its encryption code with Protect PDF)",
                }),

            ["PDF to Word"] = new ToolDoc(
                "Extracts a PDF's page text into a new .docx as plain paragraphs.",
                new[]
                {
                    "Extracts text via PdfPig, one paragraph per line of extracted text",
                    "Fonts, columns, tables, images, and scanned (image-only) pages are not preserved",
                }),

            ["PDF to Markdown"] = new ToolDoc(
                "Extracts a PDF's page text into a new .md file as plain paragraphs, one horizontal rule between pages.",
                new[]
                {
                    "Extracts text via PdfPig, the same engine PDF to Word uses",
                    "Fonts, headings, columns, tables, images, and scanned (image-only) pages are not preserved",
                }),

            ["Merge PDFs"] = new ToolDoc(
                "Combines multiple PDF files into one, in an order you control.",
                new[]
                {
                    "Add any number of PDF files, then reorder them with Move Up/Move Down before merging",
                    "Pages are copied byte-for-byte via PdfSharp's page-import mechanism, not re-rendered",
                }),

            ["Split PDF"] = new ToolDoc(
                "Splits a PDF two ways: extract a page range into one new file, or break every page out into its own file.",
                new[]
                {
                    "Extract mode saves one new PDF containing just the page range you specify",
                    "Split-every-page mode writes one single-page PDF per page into a folder you choose",
                    "Shows the source PDF's page count up front so you know what range is valid",
                }),

            ["Rotate PDF Pages"] = new ToolDoc(
                "Rotates all (or just the pages you specify) of a PDF's pages by 90, 180, or 270 degrees.",
                new[]
                {
                    "Optionally target specific pages with a spec like \"1,3,5-7\" - leave it blank to rotate every page",
                    "Sets the page's /Rotate flag rather than re-rendering content, so it's lossless regardless of what the page contains",
                }),

            ["Add Page Numbers"] = new ToolDoc(
                "Stamps a \"Page X of N\" label onto every page of a PDF.",
                new[]
                {
                    "Choose from 6 positions: bottom/top, each left/center/right",
                    "Stamps into the existing page content rather than replacing it, so nothing else on the page is disturbed",
                }),

            ["Add/Remove Watermark"] = new ToolDoc(
                "Stamps a diagonal, semi-transparent text watermark onto every page of a PDF - and offers a best-effort watermark removal mode.",
                new[]
                {
                    "Add mode: choose the watermark text, opacity, and rotation angle",
                    "Remove mode searches each page's actual content stream for text matching a string you provide and strips just that text - not a cosmetic overlay",
                    "Removal reliably finds plain text drawn with a simple, non-subsetted font (including watermarks this tool itself adds), but can't remove an image-based watermark or text drawn with a subsetted/custom-encoded embedded font",
                }),

            ["Protect PDF (Add Password)"] = new ToolDoc(
                "Adds password protection to an unencrypted PDF and saves an encrypted copy.",
                new[]
                {
                    "Set a user password (required to open), an owner password (required to change permissions), or both",
                    "Shares its encryption code with Word to PDF's optional password-protect step",
                }),

            ["Compress PDF"] = new ToolDoc(
                "Shrinks a PDF by re-encoding its embedded JPEG images at a lower quality, then re-saving the document.",
                new[]
                {
                    "3 presets matching ilovepdf.com's pattern: Low compression (high quality, ~85), Medium (~60), High compression (low quality, ~35)",
                    "Only recompresses images whose PDF filter is DCTDecode (already JPEG) - other filters and non-image content are left untouched",
                    "Shows before/after file size and how many embedded images were actually recompressed vs. left as-is",
                    "A pure vector/text PDF, or one whose images are already highly compressed, will shrink little or not at all",
                }),

            ["Code Runner"] = new ToolDoc(
                "Runs a block of code using whichever supported language toolchain is actually installed on this machine, and shows what it printed. Shells out to a real interpreter/compiler rather than bundling a scripting engine, so it runs directly on this machine - treat it the same as running the code yourself in a terminal, not as a sandboxed judge.",
                new[]
                {
                    "Supports PowerShell (pwsh, falling back to Windows PowerShell), Python, JavaScript (Node.js), Batch (cmd), Java (JDK 11+ single-file source-launcher), R (Rscript), and C/C++ (via gcc/g++)",
                    "HTML doesn't run as a process at all - it opens directly in your default browser instead",
                    "C/C++ compile first - the build's own output and a separate build timeout are shown before the program runs, and a build failure is shown as a distinct \"BUILD FAILED\" result rather than attempting to run anything",
                    "The language dropdown marks any toolchain it can't actually find on PATH as \"(not found)\", with a Recheck button for after you install one",
                    "A configurable timeout (1-60s, default 10) kills a runaway script - stdout, stderr, exit code, and elapsed time are all shown after every run",
                    "Choose File loads an existing script and auto-selects its matching language from the file extension",
                }),

            ["Compress Image"] = new ToolDoc(
                "Shrinks an image by re-encoding it as a JPEG at a reduced quality.",
                new[]
                {
                    "Same 3 presets as Compress PDF: Low/Medium/High compression",
                    "Accepts JPEG, PNG, BMP, GIF, or TIFF as input - output is always JPEG",
                    "A transparent PNG/GIF is flattened onto a white background first, since JPEG has no alpha channel",
                    "Shows before/after file size",
                }),

            ["Url Encoder & Decoder"] = new ToolDoc(
                "Encodes or decodes a string so that it conforms to the Uniform Resource Locators specification (URL, RFC 1738/3986) - characters outside the allowed set are encoded as a percent-escaped hex value.",
                new[]
                {
                    "URL-encodes text, or decodes a URL-encoded string, in either direction",
                    "Supports copy-paste or file upload",
                }),

            ["Base 64 Encoder & Decoder"] = new ToolDoc(
                "Encodes or decodes a UTF-8 string so that it conforms to the Base64 Data Encodings specification (RFC 4648).",
                new[]
                {
                    "Base64-encodes text, or decodes a Base64 string, in either direction",
                    "Supports copy-paste or file upload",
                }),

            ["Base 32 Encoder & Decoder"] = new ToolDoc(
                "Encodes or decodes a UTF-8 string so that it conforms to the Base32 Data Encodings specification (RFC 4648) - the encoding used by things like TOTP secret keys.",
                new[]
                {
                    "Base32-encodes text, or decodes a Base32 string, in either direction",
                    "Case-insensitive on decode, and accepts input with or without '=' padding",
                }),

            ["Image / Data URI Previewer"] = new ToolDoc(
                "Previews an image from a pasted data URI or bare base64 string, or converts an image file into its own base64/data URI - useful for embedding small images directly in HTML/CSS.",
                new[]
                {
                    "Paste a \"data:image/png;base64,...\" URI, or a bare base64 string, to see it rendered live",
                    "Choose an image file to get its data URI - fills the paste box so the preview and both copy buttons work the same way either direction",
                    "Shows the detected format, dimensions, and decoded byte size",
                    "Copy Data URI (the full string) or Copy Base64 Only, whichever you need",
                }),

            ["Convert File Encoding"] = new ToolDoc(
                "Changes the encoding of a text file to another one - for example from ISO-8859-1 to UTF-8, or from UTF-8 to UTF-16.",
                new[]
                {
                    "Reads the source file using a chosen encoding and previews the decoded text",
                    "Re-saves it under a chosen target encoding via a Save As dialog",
                    "You choose the source encoding - the tool doesn't auto-detect it",
                }),

            ["Message Digester (MD5, SHA-256, SHA-512)"] = new ToolDoc(
                "Computes a digest from a string using a chosen hash algorithm.",
                new[]
                {
                    "Supports MD5, SHA-256 and SHA-512",
                    "Outputs the digest as lowercase hexadecimal",
                }),

            ["HMAC Generator"] = new ToolDoc(
                "Computes a Hash-based Message Authentication Code (HMAC) for a message using a secret key you provide.",
                new[]
                {
                    "Supports HMAC-MD5, HMAC-SHA1, HMAC-SHA256 and HMAC-SHA512",
                    "Outputs the HMAC as lowercase hexadecimal",
                    "\"Generate Random Key\" fills Secret Key with a cryptographically random key sized for the selected algorithm - the same output format as `openssl rand -hex N`",
                }),

            ["QR Code Generator"] = new ToolDoc(
                "Generates a QR code image from any text or URL.",
                new[]
                {
                    "Renders the QR code as a PNG you can save to disk",
                }),

            ["GUID Generator"] = new ToolDoc(
                "Generates random GUIDs (UUID v4).",
                new[]
                {
                    "Generates 1 to 1000 GUIDs at a time",
                    "Configurable hyphens, uppercase, and brace-wrapping",
                }),

            ["Password Generator"] = new ToolDoc(
                "Generates a random password or a word-based passphrase using a cryptographically secure random number generator, with a live strength estimate based on the generated value's entropy.",
                new[]
                {
                    "Password mode: 4 characters up to a configurable max (Settings > Password Generator, default 99, hard cap 128), with independently toggled lowercase, uppercase, numbers and symbols - guarantees at least one of every selected type",
                    "Passphrase mode: 2-12 words from a 128-word bank, with optional capitalization, an appended digit, and a choice of separator character",
                    "Strength meter (Weak / Fair / Strong) is computed from the actual entropy of the chosen options, not just length",
                    "Regenerates instantly whenever any option changes, and copies to the clipboard with one click - copying auto-clears the clipboard after a configurable delay (Settings, default 30s, 0 = off)",
                    "History (DPAPI-encrypted, per Windows user) records values from explicit Generate clicks - not every live options change; entry count is configurable in Settings (default 50, 0 = off)",
                }),

            ["JavaScript Beautifier"] = new ToolDoc(
                "Formats your JS file for readability.",
                new[]
                {
                    "Reformats minified or compact JavaScript into indented, multi-line code",
                    "Preserves variable/function names rather than renaming them",
                }),

            ["JavaScript Minifier"] = new ToolDoc(
                "Compresses a JavaScript string/file with no intended change in behavior.",
                new[]
                {
                    "Removes unnecessary whitespace, indentation and line breaks",
                    "Renames local variables to shorter names and updates their references accordingly",
                }),

            ["CSS Beautifier"] = new ToolDoc(
                "Formats your CSS file for readability.",
                new[]
                {
                    "Reformats minified or compact CSS into indented, multi-line rules",
                }),

            ["CSS Minifier"] = new ToolDoc(
                "Compresses a CSS string/file with no intended change in behavior.",
                new[]
                {
                    "Removes unnecessary whitespace, comments and line breaks",
                    "Applies standard CSS-safe shrinking (e.g. shorter color/measure values) where applicable",
                }),

            ["String Utilities"] = new ToolDoc(
                "A small set of common string transforms.",
                new[]
                {
                    "Convert to UPPERCASE, lowercase, or Title Case",
                    "Convert to camelCase, PascalCase, snake_case, or kebab-case - recognizes word boundaries from spaces, underscores, hyphens, and existing camelCase/acronym casing, so it re-cases identifiers as well as prose",
                    "Convert to a URL Slug - transliterates accented characters (e.g. é -> e) and strips everything else down to a-z0-9 and hyphens",
                    "Trim & collapse whitespace, and remove blank lines",
                    "Reverse a string",
                    "A live stats report: characters, words, unique words, spaces, sentences, paragraphs, lines, pages, and reading/speaking time - updates as you type",
                }),

            ["Text/JSON/XML Diff Viewer"] = new ToolDoc(
                "Compares two blocks of text line by line and highlights what was added or removed, with an optional mode that pretty-prints both sides as JSON or XML first so formatting differences don't create false noise.",
                new[]
                {
                    "Plain Text, JSON, and XML comparison modes",
                    "Highlights added lines in green and removed lines in red, with a live added/removed count",
                    "Updates live as you type in either box",
                }),

            ["Color Picker"] = new ToolDoc(
                "Picks a color visually from a saturation/value gradient square and a hue slider, or samples one directly off a loaded image, rather than typing one in.",
                new[]
                {
                    "Drag inside the gradient square to set saturation and brightness for the currently selected hue",
                    "Drag the hue slider to change which hue the gradient square renders",
                    "Load an image and click anywhere on it to sample that pixel's exact color, or use Pick from Screen to sample any pixel on your monitor",
                    "Harmony dropdown (Complementary, Analogous, Triadic, Split-Complementary, Tetradic) generates a swatch strip of related colors from the current hue - click a swatch to jump to it",
                    "Shows a live swatch, with the value readable and copyable as Hex, RGB, HSL, HSV, or OKLCH",
                }),

            ["Color Converter"] = new ToolDoc(
                "Converts a color between HEX, RGB and HSL, showing a live swatch preview.",
                new[]
                {
                    "Accepts #RGB / #RRGGBB hex, rgb(r, g, b), or hsl(h, s%, l%) as input",
                    "Shows all three representations at once, each with its own Copy button",
                    "Updates live as you type",
                }),

            ["JWT Decoder"] = new ToolDoc(
                "Decodes a JSON Web Token's header and payload without needing the signing key, and shows when it was issued and when it expires.",
                new[]
                {
                    "Decodes and pretty-prints the header and payload JSON",
                    "Summarizes iat/nbf/exp claims and flags an expired token",
                    "Optionally verifies HS256/HS384/HS512 signatures if you provide the shared secret",
                    "Updates live as you type",
                }),

            ["JWT Encoder"] = new ToolDoc(
                "Builds and signs a JSON Web Token from a JSON claims payload and a secret key - the reverse of the JWT Decoder.",
                new[]
                {
                    "Starts prefilled with an example payload including a live \"iat\" (issued-at) timestamp",
                    "Signs with HS256, HS384, or HS512 using a secret key you provide",
                    "Produces the standard three-part, dot-separated compact token",
                    "\"Generate Random Key\" fills Secret Key with a cryptographically random key sized for the selected algorithm - the same output format as `openssl rand -hex N`",
                }),

            ["AES Encrypt / Decrypt"] = new ToolDoc(
                "Encrypts text with a password using AES-256-GCM, or decrypts a previously-encrypted blob back to plain text with the same password.",
                new[]
                {
                    "Output is a single self-contained Base64 string - a fresh random salt and nonce are embedded alongside the ciphertext, so there's nothing else to remember besides the password",
                    "Uses authenticated encryption (AES-GCM): a wrong password or corrupted/tampered ciphertext fails with a clear error instead of silently producing garbage",
                    "Encrypting the same text twice produces different output each time (fresh salt/nonce per run) - this is expected, not a bug",
                }),

            ["Certificate Decoder"] = new ToolDoc(
                "Decodes an X.509 certificate - paste it as PEM or base64 DER, or choose a .cer/.crt/.pem file - and shows its key details.",
                new[]
                {
                    "Shows subject, issuer, serial number, SHA-1 thumbprint, signature algorithm, and public key algorithm/size",
                    "Shows the validity window (Not Before / Not After) and flags a certificate as Valid, Expired, or Not Yet Valid",
                    "Accepts pasted PEM text, pasted base64 DER, or an uploaded certificate file",
                }),

            ["HTML Escape"] = new ToolDoc(
                "Escapes or unescapes an HTML string, removing traces of characters that could be misinterpreted as markup.",
                new[]
                {
                    "Escapes reserved characters (', \", &, <, >) and other characters with HTML entity equivalents",
                    "Unescapes HTML entities back to plain text",
                }),

            ["XML Escape"] = new ToolDoc(
                "Escapes or unescapes an XML string, removing traces of characters that could be misinterpreted as markup - or wraps/extracts a CDATA section.",
                new[]
                {
                    "Escapes the 5 reserved XML characters (&, <, >, \", ')",
                    "Unescapes them, including numeric character references, back to plain text",
                    "Wrap in CDATA surrounds the text with <![CDATA[ ... ]]>, correctly splitting any literal \"]]>\" so it can't prematurely close the section",
                    "Extract from CDATA reverses it, erroring if the input isn't wrapped that way",
                }),

            ["Java and .Net Escape"] = new ToolDoc(
                "Escapes or unescapes a Java or .NET string literal body, removing traces of characters that could prevent it compiling.",
                new[]
                {
                    "Escapes backslash, double-quote, newline, carriage return and tab",
                    "Unescapes those sequences back to raw text",
                }),

            ["JavaScript Escape"] = new ToolDoc(
                "Escapes or unescapes a JavaScript string literal body, removing traces of characters that could prevent it being interpreted.",
                new[]
                {
                    "Escapes backslash, both quote characters, newline, carriage return and tab",
                    "Unescapes those sequences back to raw text",
                }),

            ["JSON Escape"] = new ToolDoc(
                "Escapes or unescapes a JSON string, removing traces of characters that could prevent it parsing.",
                new[]
                {
                    "Escape produces a full, quoted JSON string token ready to paste into a JSON document",
                    "Unescape takes a quoted JSON string token and returns the raw text",
                }),

            ["CSV Escape"] = new ToolDoc(
                "Escapes or unescapes a single CSV field, removing traces of characters that could prevent it parsing.",
                new[]
                {
                    "Escape quotes the value (and doubles internal quotes) only when the field actually needs it",
                    "Unescape strips quoting from an already-quoted field",
                }),

            ["SQL Escape"] = new ToolDoc(
                "Escapes or unescapes a SQL string, removing traces of characters that could prevent it parsing as a string literal.",
                new[]
                {
                    "Escape doubles single quotes for safe use inside a SQL string literal",
                    "Unescape reverses it",
                }),

            ["Lorem Ipsum Generator"] = new ToolDoc(
                "Lets you choose how many paragraphs of placeholder Lorem Ipsum text you want.",
                new[]
                {
                    "Generates 1 to 50 paragraphs",
                    "Optionally starts with the traditional \"Lorem ipsum dolor sit amet...\" opening",
                }),

            ["HTML Viewer"] = new ToolDoc(
                "A live, side-by-side HTML editor and preview.",
                new[]
                {
                    "Syntax-highlighted, line-numbered editor on the left; rendered preview on the right",
                    "Both update on every keystroke - no button to click",
                    "\"Open in Default Browser\" gives full-fidelity rendering outside the embedded preview",
                    "The embedded preview uses the WebBrowser control (IE11 engine); for anything relying on modern browser-only features, use Open in Default Browser",
                }),

            ["Markdown Previewer"] = new ToolDoc(
                "A live, side-by-side Markdown editor and preview, rendered CommonMark-compliant.",
                new[]
                {
                    "Plain-text editor on the left, rendered HTML preview on the right - both update on every keystroke",
                    "Supports tables, footnotes, and other CommonMark + GitHub-flavored extensions",
                    "\"Open in Default Browser\" gives full-fidelity rendering outside the embedded preview",
                    "The embedded preview uses the WebBrowser control (IE11 engine); for anything relying on modern browser-only features, use Open in Default Browser",
                }),

            ["List of MIME Types"] = new ToolDoc(
                "A searchable reference list of common file extensions and their MIME types.",
                new[]
                {
                    "Covers ~70 common extensions across documents, images, audio/video, fonts and archives",
                    "Live search filters the list as you type",
                }),

            ["HTML Entities"] = new ToolDoc(
                "A searchable reference list of common named HTML character entities.",
                new[]
                {
                    "Shows each entity's name and the character it represents",
                    "Live search filters the list as you type",
                }),

            ["Url Parser / Query String Splitter"] = new ToolDoc(
                "Parses a URL into its individual components and splits the query string into a human-readable format.",
                new[]
                {
                    "Breaks a URL into scheme, host, port, path and fragment",
                    "Splits and URL-decodes every query-string parameter into a readable list",
                }),

            ["HTTP Status Codes"] = new ToolDoc(
                "A searchable reference list of HTTP status codes.",
                new[]
                {
                    "Covers the full IANA-registered range (1xx-5xx), reason phrase, and a short meaning for each",
                    "Live search filters the list as you type",
                }),

            ["I18N Standards / Locale Codes"] = new ToolDoc(
                "A searchable reference list of locale/culture codes.",
                new[]
                {
                    "Lists every culture code, name, language and region that .NET itself recognizes on this machine",
                    "Generated live from the .NET runtime's own culture list, not a hand-typed table",
                }),

            ["Internal Name Encoder / Decoder"] = new ToolDoc(
                "Converts between a SharePoint column/list display name and its \"_xHHHH_\"-encoded internal name.",
                new[]
                {
                    "Encodes any character outside plain ASCII letters/digits/underscore as _xHHHH_ (4-digit hex)",
                    "Decodes _xHHHH_ sequences back to the original characters",
                    "Covers the common case, not every SharePoint edge case (e.g. names that would collide with an existing _xHHHH_ sequence)",
                }),

            ["Claims Identity Encoder / Decoder"] = new ToolDoc(
                "Decodes a SharePoint claims-encoded identity string (e.g. \"i:0#.f|membership|user@domain.com\") into its parts, or builds one from a claim type and value.",
                new[]
                {
                    "Covers Windows, Forms-based membership/role, trusted provider (ADFS-style), Azure AD security group, Everyone, and All Users (Windows)",
                    "Decoding updates live as you type",
                    "Best-effort reference, not exhaustive - a custom trusted identity provider uses its own provider name",
                }),

            ["CAML Query Formatter"] = new ToolDoc(
                "Pretty-prints a CAML query - SharePoint's XML query language for lists - using the same formatting engine as the XML Formatter.",
                new[]
                {
                    "Formats with 2 or 4 spaces per indent level, or compacts to one line",
                    "Works on a full <View>/<Query> document or a bare fragment like <Where>...</Where>",
                }),

            ["REST API Query Reference"] = new ToolDoc(
                "A searchable reference of common SharePoint REST/OData endpoints and query parameters, with example URLs.",
                new[]
                {
                    "Covers web/site, lists, list items, files/folders, search, user profiles, and the request-digest flow needed for writes",
                    "Includes the OData query parameters ($select, $expand, $filter, $orderby, $top, paging) that combine with almost any GET endpoint",
                    "Live search filters the list as you type",
                },
                DisplayName: "SharePoint REST API Query Reference"),
        };
    }
}
