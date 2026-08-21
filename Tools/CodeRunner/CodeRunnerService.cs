using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DevToolbox.Tools.CodeRunner
{
    /// <summary>
    /// How a language actually gets executed - the 9 supported languages fall into 3 distinct
    /// execution models, not just one:
    ///  - Interpreted: one interpreter process runs the script file directly (PowerShell, Python,
    ///    Node, Batch, Java's single-file source-launcher, R). One captured stdout/stderr, one
    ///    timeout.
    ///  - Compiled: a genuine two-phase model (C, C++) - a compiler process builds a temp .exe
    ///    first (its own captured build output and its own timeout), and only if that succeeds is
    ///    the resulting .exe run as the second phase, itself timed/captured the same way an
    ///    Interpreted run is.
    ///  - OpenInBrowser: not a process execution at all (HTML) - "running" it means handing the
    ///    file to the OS shell to open in the default browser. Doesn't fit the stdout/stderr
    ///    RunResult model, so it's handled by its own OpenInBrowser() method instead of Run().
    /// </summary>
    public enum LanguageKind
    {
        Interpreted,
        Compiled,
        OpenInBrowser
    }

    /// <summary>
    /// One supported language: its display name, the temp-file extension to give it, which
    /// execution model it uses, and the model-specific details (interpreter candidates/probe/run
    /// args for Interpreted, compiler candidates/probe/compile-args for Compiled). Use the
    /// Interpreted/Compiled/Browser factory methods below rather than the private constructor -
    /// they only expose the fields that are actually meaningful for that Kind.
    /// </summary>
    public sealed class LanguageDefinition
    {
        public string Name { get; }
        public string FileExtension { get; }
        public LanguageKind Kind { get; }

        // Interpreted only - the runtime candidates to try (in priority order - only PowerShell
        // has more than one entry) and how to build that runtime's run arguments for a temp script.
        public IReadOnlyList<string> CandidateExecutables { get; }
        public IReadOnlyList<string> ProbeArguments { get; }
        public Func<string, string[]>? BuildRunArguments { get; }

        // Compiled only - the compiler candidates to try, and how to build its compile arguments
        // given the source file path and the desired output .exe path.
        public IReadOnlyList<string> CompilerCandidateExecutables { get; }
        public IReadOnlyList<string> CompilerProbeArguments { get; }
        public Func<string, string, string[]>? BuildCompileArguments { get; }

        private LanguageDefinition(string name, string fileExtension, LanguageKind kind,
            string[] candidateExecutables, string[] probeArguments, Func<string, string[]>? buildRunArguments,
            string[] compilerCandidateExecutables, string[] compilerProbeArguments, Func<string, string, string[]>? buildCompileArguments)
        {
            Name = name;
            FileExtension = fileExtension;
            Kind = kind;
            CandidateExecutables = candidateExecutables;
            ProbeArguments = probeArguments;
            BuildRunArguments = buildRunArguments;
            CompilerCandidateExecutables = compilerCandidateExecutables;
            CompilerProbeArguments = compilerProbeArguments;
            BuildCompileArguments = buildCompileArguments;
        }

        public static LanguageDefinition Interpreted(string name, string fileExtension,
            string[] candidateExecutables, string[] probeArguments, Func<string, string[]> buildRunArguments)
            => new(name, fileExtension, LanguageKind.Interpreted,
                candidateExecutables, probeArguments, buildRunArguments,
                Array.Empty<string>(), Array.Empty<string>(), null);

        public static LanguageDefinition Compiled(string name, string fileExtension,
            string[] compilerCandidateExecutables, string[] compilerProbeArguments, Func<string, string, string[]> buildCompileArguments)
            => new(name, fileExtension, LanguageKind.Compiled,
                Array.Empty<string>(), Array.Empty<string>(), null,
                compilerCandidateExecutables, compilerProbeArguments, buildCompileArguments);

        public static LanguageDefinition Browser(string name, string fileExtension)
            => new(name, fileExtension, LanguageKind.OpenInBrowser,
                Array.Empty<string>(), Array.Empty<string>(), null,
                Array.Empty<string>(), Array.Empty<string>(), null);
    }

    /// <summary>
    /// The result of one Run() call (Interpreted or Compiled languages) - always returned, even on
    /// timeout or a not-found interpreter/compiler, rather than throwing, so the UI has one shape
    /// to render. BuildStdout/BuildStderr/BuildFailed/BuildExitCode are left at their default
    /// (null/false/null) for Interpreted languages - only a Compiled language populates them, and
    /// the UI uses "BuildStdout is not null" as the signal that this result has a distinct build
    /// phase to render separately from the program's own output.
    /// </summary>
    public sealed record RunResult(
        string Stdout,
        string Stderr,
        int? ExitCode,
        bool TimedOut,
        string? BuildStdout = null,
        string? BuildStderr = null,
        bool BuildFailed = false,
        int? BuildExitCode = null);

    /// <summary>The result of the OpenInBrowser() case - not a process run at all, so it doesn't share RunResult's shape.</summary>
    public sealed record OpenInBrowserResult(bool Success, string Message);

    /// <summary>
    /// Pure logic (no WinForms references) for the Code Runner tool: shells out to whichever
    /// interpreter/compiler is actually installed on this machine to execute user-supplied code -
    /// no bundled compiler/runtime, no sandboxing. This is a "run this like you'd run it yourself
    /// in a terminal" convenience, not an isolated execution environment.
    /// </summary>
    public static class CodeRunnerService
    {
        // A compiled language's build phase gets its own, longer-than-typical-script timeout -
        // compiling can legitimately take longer than the kind of short script this tool is meant
        // for, and it's a separate concern from the timeoutSeconds the caller picked for running
        // the compiled program itself.
        private const int CompileTimeoutSeconds = 30;

        /// <summary>The 9 supported languages, in dropdown display order.</summary>
        public static readonly LanguageDefinition[] Languages =
        {
            LanguageDefinition.Interpreted(
                "PowerShell", ".ps1",
                new[] { "pwsh.exe", "powershell.exe" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "exit" },
                scriptPath => new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptPath }),

            LanguageDefinition.Interpreted(
                "Python", ".py",
                new[] { "python.exe" },
                new[] { "--version" },
                scriptPath => new[] { scriptPath }),

            LanguageDefinition.Interpreted(
                "JavaScript (Node.js)", ".js",
                new[] { "node.exe" },
                new[] { "--version" },
                scriptPath => new[] { scriptPath }),

            LanguageDefinition.Interpreted(
                "Batch (cmd)", ".bat",
                new[] { "cmd.exe" },
                new[] { "/c", "exit" },
                scriptPath => new[] { "/c", scriptPath }),

            // JEP 330 single-file source-launcher ("java Foo.java" compiles in memory and runs it
            // directly, no separate javac step, and - unlike a normal javac compile - doesn't
            // require the file name to match the public class name). Requires JDK 11+; there's
            // deliberately no javac-then-java fallback for older JDKs here, so a JDK 8/10 install
            // (java.exe present but the launcher unsupported) will resolve as "available" but fail
            // at run time with the JDK's own "error: option not supported" style message - not
            // worth the added complexity of a separate detection path for that older case.
            LanguageDefinition.Interpreted(
                "Java", ".java",
                new[] { "java.exe" },
                new[] { "--version" },
                scriptPath => new[] { scriptPath }),

            // No interpreter/compiler at all - see OpenInBrowser() below. Always "available"
            // (every Windows machine has a default browser), so IsAvailable() short-circuits
            // before ever reaching the interpreter-resolution cache for this one.
            LanguageDefinition.Browser("HTML", ".html"),

            LanguageDefinition.Interpreted(
                "R", ".R",
                new[] { "Rscript.exe" },
                new[] { "--version" },
                scriptPath => new[] { scriptPath }),

            // MSVC's cl.exe is deliberately not attempted here - it only works from inside a
            // Visual Studio developer command prompt (vcvarsall.bat has to run first to put it on
            // PATH and set several env vars), which isn't a simple direct process invocation like
            // every other interpreter/compiler this tool uses. gcc/g++ (MinGW-w64 or similar) is a
            // normal standalone PATH executable, so that's the only C/C++ toolchain supported.
            LanguageDefinition.Compiled(
                "C", ".c",
                new[] { "gcc.exe" },
                new[] { "--version" },
                (sourcePath, exePath) => new[] { sourcePath, "-o", exePath }),

            LanguageDefinition.Compiled(
                "C++", ".cpp",
                new[] { "g++.exe" },
                new[] { "--version" },
                (sourcePath, exePath) => new[] { sourcePath, "-o", exePath }),
        };

        // Which real executable each language resolved to (the interpreter for Interpreted, the
        // compiler for Compiled - or null if none of its candidates could actually be started),
        // cached per language name so the UI isn't re-probing a process launch on every
        // paint/dropdown-open. Lazy<T> gives thread-safe "compute once" for free;
        // RecheckAvailability() below drops the cache so a later explicit recheck re-probes from
        // scratch (e.g. after the user just installed Python and clicks Recheck).
        private static readonly Dictionary<string, Lazy<string?>> ResolvedExecutableCache = new();
        private static readonly object CacheLock = new();

        /// <summary>True if this language can actually run right now: always true for OpenInBrowser (no toolchain needed), otherwise whether its interpreter/compiler chain resolves to something launchable (cached - see RecheckAvailability to force a fresh probe).</summary>
        public static bool IsAvailable(LanguageDefinition language)
            => language.Kind == LanguageKind.OpenInBrowser || ResolveExecutable(language) is not null;

        /// <summary>The actual executable name this language resolved to (the interpreter for Interpreted, the compiler for Compiled), or null if none of its candidates could be started. Not meaningful for OpenInBrowser languages.</summary>
        public static string? ResolveExecutable(LanguageDefinition language)
        {
            Lazy<string?> lazy;
            lock (CacheLock)
            {
                if (!ResolvedExecutableCache.TryGetValue(language.Name, out lazy!))
                {
                    lazy = new Lazy<string?>(() => ProbeExecutables(language));
                    ResolvedExecutableCache[language.Name] = lazy;
                }
            }
            return lazy.Value;
        }

        /// <summary>Drops every cached availability result, so the next IsAvailable/ResolveExecutable call re-probes from scratch (e.g. a UI "Recheck" button after installing a toolchain).</summary>
        public static void RecheckAvailability()
        {
            lock (CacheLock) ResolvedExecutableCache.Clear();
        }

        /// <summary>Tries each candidate executable (interpreter or compiler, depending on Kind) in order, returning the first one that can actually be started with its harmless probe arguments.</summary>
        private static string? ProbeExecutables(LanguageDefinition language)
        {
            var candidates = language.Kind == LanguageKind.Compiled ? language.CompilerCandidateExecutables : language.CandidateExecutables;
            var probeArgs = language.Kind == LanguageKind.Compiled ? language.CompilerProbeArguments : language.ProbeArguments;

            foreach (var exe in candidates)
            {
                if (CanStart(exe, probeArgs)) return exe;
            }
            return null;
        }

        /// <summary>
        /// Attempts to actually start the executable with the given (harmless, quick-exit)
        /// arguments - the same mechanism Run()/ExecuteProcess() below use for the real
        /// execution, so "detected as available" and "actually runnable" can never disagree. A
        /// Win32Exception is the genuine "Windows couldn't find/launch this executable at all"
        /// signal; any other failure is treated the same way here since this is only a
        /// best-effort probe.
        /// </summary>
        private static bool CanStart(string executable, IReadOnlyList<string> args)
        {
            try
            {
                var psi = new ProcessStartInfo(executable)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                foreach (var arg in args) psi.ArgumentList.Add(arg);

                using var process = Process.Start(psi);
                if (process is null) return false;

                // Drain both redirected streams asynchronously before waiting - the same
                // deadlock-avoidance rule as ExecuteProcess() below, even though these probe
                // commands shouldn't produce much output.
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exited = process.WaitForExit(5000);
                if (!exited)
                {
                    try { process.Kill(entireProcessTree: true); } catch (Exception) { /* best-effort cleanup of a stuck probe */ }
                    return false;
                }
                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
            catch (Exception)
            {
                // Any other launch failure is treated as "not available" too - this is only a
                // best-effort probe, not the real run, so it never throws.
                return false;
            }
        }

        /// <summary>
        /// Runs an Interpreted or Compiled language. Writes <paramref name="code"/> to a fresh
        /// temp file, then either runs it directly with the resolved interpreter (Interpreted), or
        /// compiles it first and only runs the result if that succeeds (Compiled). Always returns
        /// a RunResult - never throws for a missing interpreter/compiler or a timed-out/failed
        /// script, since both are normal, expected outcomes the UI needs to display, not
        /// exceptional ones. OpenInBrowser languages must use OpenInBrowser() instead - this
        /// throws if called with one, since there's no stdout/stderr/exit-code model for it.
        /// </summary>
        public static RunResult Run(LanguageDefinition language, string code, int timeoutSeconds)
        {
            if (language.Kind == LanguageKind.OpenInBrowser)
            {
                throw new InvalidOperationException($"{language.Name} doesn't run as a process - call OpenInBrowser() instead.");
            }

            return language.Kind == LanguageKind.Compiled
                ? RunCompiled(language, code, timeoutSeconds)
                : RunInterpreted(language, code, timeoutSeconds);
        }

        private static RunResult RunInterpreted(LanguageDefinition language, string code, int timeoutSeconds)
        {
            var executable = ResolveExecutable(language);
            if (executable is null)
            {
                return new RunResult(string.Empty, $"{language.Name} isn't installed or isn't on PATH.", null, TimedOut: false);
            }

            var tempDir = CreateTempDir();
            try
            {
                var scriptPath = Path.Combine(tempDir, "script" + language.FileExtension);
                WriteScript(scriptPath, code);

                var arguments = language.BuildRunArguments!(scriptPath);
                var (stdout, stderr, exitCode, timedOut) = ExecuteProcess(executable, arguments, tempDir, timeoutSeconds);
                return new RunResult(stdout, stderr, exitCode, timedOut);
            }
            finally
            {
                DeleteTempDir(tempDir);
            }
        }

        private static RunResult RunCompiled(LanguageDefinition language, string code, int timeoutSeconds)
        {
            var compiler = ResolveExecutable(language);
            if (compiler is null)
            {
                return new RunResult(string.Empty, string.Empty, null, TimedOut: false,
                    BuildStdout: string.Empty,
                    BuildStderr: $"{language.Name} compiler isn't installed or isn't on PATH.",
                    BuildFailed: true);
            }

            var tempDir = CreateTempDir();
            try
            {
                var sourcePath = Path.Combine(tempDir, "script" + language.FileExtension);
                var exePath = Path.Combine(tempDir, "script.exe");
                WriteScript(sourcePath, code);

                var compileArguments = language.BuildCompileArguments!(sourcePath, exePath);
                var (buildStdout, buildStderr, buildExitCode, buildTimedOut) =
                    ExecuteProcess(compiler, compileArguments, tempDir, CompileTimeoutSeconds);

                if (buildTimedOut)
                {
                    var stderr = AppendNote(buildStderr, $"Compilation timed out after {CompileTimeoutSeconds}s.");
                    return new RunResult(string.Empty, string.Empty, null, TimedOut: false,
                        BuildStdout: buildStdout, BuildStderr: stderr, BuildFailed: true, BuildExitCode: null);
                }

                if (buildExitCode != 0 || !File.Exists(exePath))
                {
                    return new RunResult(string.Empty, string.Empty, null, TimedOut: false,
                        BuildStdout: buildStdout, BuildStderr: buildStderr, BuildFailed: true, BuildExitCode: buildExitCode);
                }

                // Build succeeded - run the produced .exe as its own phase, using the exact same
                // timed/captured execution as an Interpreted language would.
                var (stdout, stderr2, exitCode, timedOut) = ExecuteProcess(exePath, Array.Empty<string>(), tempDir, timeoutSeconds);
                return new RunResult(stdout, stderr2, exitCode, timedOut,
                    BuildStdout: buildStdout, BuildStderr: buildStderr, BuildFailed: false, BuildExitCode: buildExitCode);
            }
            finally
            {
                DeleteTempDir(tempDir);
            }
        }

        /// <summary>
        /// HTML doesn't fit the stdout/stderr process model at all - "running" it means opening
        /// the file in the OS's default browser, not executing something whose output this tool
        /// captures. Writes the code to a temp .html file and hands it to the shell
        /// (UseShellExecute = true) rather than launching a specific browser executable directly.
        /// Deliberately does NOT clean up its temp file/directory the way Run() does - the browser
        /// needs the file to still exist after this method returns, since UseShellExecute hands
        /// off to the OS asynchronously and there's no "the browser is done with it now" signal to
        /// hook a cleanup to. The file is left under %TEMP% for the OS/user to reclaim eventually,
        /// same as any other one-off temp file.
        /// </summary>
        public static OpenInBrowserResult OpenInBrowser(string code)
        {
            try
            {
                var tempDir = CreateTempDir();
                var htmlPath = Path.Combine(tempDir, "page.html");
                WriteScript(htmlPath, code);

                using var process = Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
                return new OpenInBrowserResult(true, "Opened in your default browser.");
            }
            catch (Exception ex)
            {
                return new OpenInBrowserResult(false, $"Couldn't open in a browser: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts <paramref name="executable"/> with the given individual argument values (via
        /// ProcessStartInfo.ArgumentList, which handles all the Windows quoting/escaping rules
        /// itself - no manual escaping needed), redirecting and asynchronously draining both
        /// stdout and stderr, and enforces
        /// <paramref name="timeoutSeconds"/>. The single shared execution core for every process
        /// this tool runs for real (probing aside): an Interpreted language's script, a Compiled
        /// language's compiler invocation, and a Compiled language's resulting .exe all go through
        /// here.
        ///
        /// BeginOutputReadLine/BeginErrorReadLine are wired up BEFORE WaitForExit - reading both
        /// redirected streams asynchronously like this is required to avoid the classic Process
        /// deadlock: synchronously calling WaitForExit() first can hang forever if the child
        /// writes enough output to fill the OS pipe buffer before it exits, because nothing is
        /// draining that buffer for it to keep writing into.
        ///
        /// Kill(entireProcessTree: true) terminates this process AND every child process it
        /// spawned (e.g. a PowerShell script that shells out to something else, or Node
        /// launching a subprocess) - this overload only exists on modern .NET (added in .NET
        /// Core 3.0+, never available on the .NET Framework 4.7.2 this app originally targeted;
        /// back then only the parameterless Kill() existed, which left orphaned children running
        /// after a timeout - now fixed as a direct benefit of the net10.0 migration).
        ///
        /// Captured stdout/stderr is run through StripAnsi before being returned - NO_COLOR (set
        /// below) stops most well-behaved tools from emitting ANSI color codes in the first place,
        /// but it's only a request, not a guarantee every interpreter/compiler actually honors, so
        /// stripping is the defense-in-depth half of the fix: even a tool that ignores NO_COLOR (or
        /// colors output for another reason entirely) can't leave literal escape-code garbage in
        /// what this tool displays.
        /// </summary>
        private static (string Stdout, string Stderr, int? ExitCode, bool TimedOut) ExecuteProcess(
            string executable, IReadOnlyList<string> arguments, string workingDirectory, int timeoutSeconds)
        {
            var psi = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var arg in arguments) psi.ArgumentList.Add(arg);

            // PowerShell 7's $PSStyle colorizes error records with raw ANSI escape sequences even
            // when stderr is redirected (not just when writing to a real interactive terminal) -
            // confirmed directly: a plain Write-Error came through as
            // "[31;1mWrite-Error: ...[0m" in the captured stderr text, which would
            // otherwise show up as literal garbage escape codes in this tool's output pane. NO_COLOR
            // (see no-color.org) is a convention pwsh 7.2+ and many other CLI tools (Node's chalk,
            // various npm/pip tooling, etc.) already respect to suppress exactly this, so it's set
            // unconditionally for every language here rather than only PowerShell - harmless for
            // tools that don't look at it, and a genuine improvement for any of them that do.
            psi.EnvironmentVariables["NO_COLOR"] = "1";

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = process.WaitForExit(Math.Max(1, timeoutSeconds) * 1000);
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (Exception) { /* may have exited in the tiny window between WaitForExit timing out and here */ }

                // Give the kill a moment to flush whatever partial output was already buffered
                // before this returns, rather than racing the DataReceived handlers above.
                process.WaitForExit(2000);
                return (StripAnsi(stdout.ToString()), StripAnsi(stderr.ToString()), null, true);
            }

            return (StripAnsi(stdout.ToString()), StripAnsi(stderr.ToString()), process.ExitCode, false);
        }

        private static string CreateTempDir()
        {
            // Its own subfolder under the temp dir so cleanup is one recursive delete, rather than
            // tracking individual files - matters for languages like Python (__pycache__) or a C
            // compile (intermediate .o/.exe) that can leave more than just the source file behind.
            var tempDir = Path.Combine(Path.GetTempPath(), "DevToolbox-CodeRunner-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        private static void DeleteTempDir(string tempDir)
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception) { /* best-effort temp cleanup - a lingering folder under %TEMP% isn't worth failing the run over */ }
        }

        private static void WriteScript(string path, string code)
        {
            // No BOM - a UTF-8 BOM in front of a shebang-less script confuses some interpreters
            // (notably older cmd.exe batch parsing) into misreading the first line.
            File.WriteAllText(path, code ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static string AppendNote(string text, string note) => string.IsNullOrEmpty(text) ? note : text + Environment.NewLine + note;

        // Matches ANSI/VT100 CSI escape sequences (ESC '[' ... final-letter) - e.g. "\x1b[31;1m"
        // (set red+bold) or "\x1b[0m" (reset). This is what PowerShell 7's $PSStyle, Node's chalk,
        // and many other CLI tools emit for colored output; a plain WinForms TextBox/RichTextBox
        // doesn't interpret them; they'd otherwise show up as literal garbage control-character
        // text in this tool's output pane. Only the common CSI form is stripped (covers everything
        // observed from PowerShell/Node/Python in practice) - not a full ANSI/VT100 parser.
        private static readonly Regex AnsiEscapeSequence = new("\u001B\\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

        private static string StripAnsi(string text) => string.IsNullOrEmpty(text) ? text : AnsiEscapeSequence.Replace(text, string.Empty);
    }
}
