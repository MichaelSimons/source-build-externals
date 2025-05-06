#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSharp.Compiler.CodeAnalysis;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Xunit.Sdk;

public abstract class FSharpAcceptanceTestAssembly(string? basePath = null) :
	AcceptanceTestAssembly(basePath)
{
	protected override IEnumerable<string> GetStandardReferences() =>
		[];

	protected override async ValueTask Compile(
		string[] code,
		params string[] references)
	{
		if (EnvironmentHelper.IsMono)
			throw SkipException.ForSkip("F# is not supported on Mono https://github.com/dotnet/fsharp/issues/14770");

		var compilerArgs = new List<string> { "fsc" };

		foreach (var codeText in code)
		{
			var sourcePath = Path.GetTempFileName() + ".fs";
			File.WriteAllText(sourcePath, codeText);
			compilerArgs.Add(sourcePath);
		}

		compilerArgs.AddRange([
			$"--out:{FileName}",
			$"--pdb:{PdbName}",
			$"--lib:\"{BasePath}\"",
			"--debug",
			"--target:library"
		]);
		compilerArgs.AddRange(GetStandardReferences().Concat(references).Select(r => $"--reference:{r}"));

		var checker = FSharpChecker.Create(
			FSharpOption<int>.None,
			FSharpOption<bool>.None,
			FSharpOption<bool>.None,
#pragma warning disable CS0618
			FSharpOption<LegacyReferenceResolver>.None,
#pragma warning restore CS0618
			FSharpOption<FSharpFunc<Tuple<string, DateTime>, FSharpOption<Tuple<object, IntPtr, int>>>>.None,
			FSharpOption<bool>.None,
			FSharpOption<bool>.None,
			FSharpOption<bool>.None,
			FSharpOption<bool>.None,
			FSharpOption<bool>.None,
			FSharpOption<bool>.None
		);

		var resultFSharpAsync = checker.Compile(compilerArgs.ToArray(), FSharpOption<string>.None);
		var result = await FSharpAsync.StartAsTask(resultFSharpAsync, FSharpOption<TaskCreationOptions>.None, FSharpOption<CancellationToken>.None);
		if (result.Item2 != 0)
		{
			var errors =
				result
					.Item1
					.Select(e => $"{e.FileName}({e.StartLine},{e.StartColumn}): {(e.Severity.IsError ? "error" : "warning")} {e.ErrorNumber}: {e.Message}");

			throw new InvalidOperationException($"Compilation Failed: (BasePath = '{BasePath}', TargetFrameworkReferencePaths = [{string.Join(", ", TargetFrameworkReferencePaths.Select(p => "'" + p + "'"))}]{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
		}
	}
}

#endif
