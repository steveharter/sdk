// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Tools;
using System.CommandLine;
using System.IO;
using Microsoft.DotNet.Tools.Common;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli
{
    internal static class CommonOptions
    {
        public static Option PropertiesOption() =>
            new ForwardedOption<string[]>(new string[] { "-property", "/p" })
            {
                IsHidden = true
            }.ForwardAsProperty()
            .AllowSingleArgPerToken();

        public static Option VerbosityOption() =>
            VerbosityOption(o => $"-verbosity:{o}");

        public static Option VerbosityOption(Func<VerbosityOptions, string> format) =>
            new ForwardedOption<VerbosityOptions>(
                new string[] { "-v", "--verbosity" },
                description: CommonLocalizableStrings.VerbosityOptionDescription)
            {
                ArgumentHelpName = CommonLocalizableStrings.LevelArgumentName
            }.ForwardAsSingle(format);

        public static Option FrameworkOption(string description) =>
            new ForwardedOption<string>(
                new string[] { "-f", "--framework" },
                description)
            {
                ArgumentHelpName = CommonLocalizableStrings.FrameworkArgumentName
                    
            }.ForwardAsSingle(o => $"-property:TargetFramework={o}")
            .AddSuggestions(Suggest.TargetFrameworksFromProjectFile());

        public static Option RuntimeOption(string description, bool withShortOption = true) =>
            new ForwardedOption<string>(
                withShortOption ? new string[] { "-r", "--runtime" } : new string[] { "--runtime" },
                description)
            {
                ArgumentHelpName = CommonLocalizableStrings.RuntimeIdentifierArgumentName
            }.ForwardAsSingle(o => $"-property:RuntimeIdentifier={o}")
            .AddSuggestions(Suggest.RunTimesFromProjectFile());

        public static Option CurrentRuntimeOption(string description) =>
            new ForwardedOption<bool>("--use-current-runtime", description)
                .ForwardAs("-property:UseCurrentRuntimeIdentifier=True");

        public static Option ConfigurationOption(string description) =>
            new ForwardedOption<string>(
                new string[] { "-c", "--configuration" },
                description)
            {
                ArgumentHelpName = CommonLocalizableStrings.ConfigurationArgumentName
            }.ForwardAsSingle(o => $"-property:Configuration={o}")
            .AddSuggestions(Suggest.ConfigurationsFromProjectFileOrDefaults());

        public static Option VersionSuffixOption() =>
            new ForwardedOption<string>(
                "--version-suffix",
                CommonLocalizableStrings.CmdVersionSuffixDescription)
            {
                ArgumentHelpName = CommonLocalizableStrings.VersionSuffixArgumentName
            }.ForwardAsSingle(o => $"-property:VersionSuffix={o}");

        public static Argument DefaultToCurrentDirectory(this Argument arg)
        {
            arg.SetDefaultValue(PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory()));
            return arg;
        }

        public static Option NoRestoreOption() =>
            new Option<bool>(
                "--no-restore",
                CommonLocalizableStrings.NoRestoreDescription);

        public static Option InteractiveMsBuildForwardOption() =>
            new ForwardedOption<bool>(
                "--interactive",
                CommonLocalizableStrings.CommandInteractiveOptionDescription)
            .ForwardAs("-property:NuGetInteractive=true");

        public static Option InteractiveOption() =>
            new Option<bool>(
                "--interactive",
                CommonLocalizableStrings.CommandInteractiveOptionDescription);

        public static Option DebugOption() => new Option<bool>("--debug");

        public static Option SelfContainedOption() =>
            new ForwardedOption<bool>(
                "--self-contained",
                CommonLocalizableStrings.SelfContainedOptionDescription)
            .ForwardAsSingle(o => $"-property:SelfContained={o}");

        public static Option NoSelfContainedOption() =>
            new ForwardedOption<bool>(
                "--no-self-contained",
                CommonLocalizableStrings.NoSelfContainedOptionDescription)
            .ForwardAs("-property:SelfContained=false");

        public static bool VerbosityIsDetailedOrDiagnostic(this VerbosityOptions verbosity)
        {
            return verbosity.Equals(VerbosityOptions.diag) ||
                verbosity.Equals(VerbosityOptions.diagnostic) ||
                verbosity.Equals(VerbosityOptions.d) ||
                verbosity.Equals(VerbosityOptions.detailed);
        }

        public static void ValidateSelfContainedOptions(bool hasSelfContainedOption, bool hasNoSelfContainedOption, bool hasRuntimeOption, IEnumerable<string> projectArgs)
        {
            if (hasSelfContainedOption && hasNoSelfContainedOption)
            {
                throw new GracefulException(CommonLocalizableStrings.SelfContainAndNoSelfContainedConflict);
            }

            if (!(hasSelfContainedOption || hasNoSelfContainedOption) && hasRuntimeOption)
            {
                projectArgs = projectArgs.Any() ? projectArgs : new string[] { Directory.GetCurrentDirectory() };
                foreach (var project in projectArgs)
                {
                    try
                    {
                        var msbuildProj = MsbuildProject.FromFileOrDirectory(new ProjectCollection(), project, false);
                        if (msbuildProj.IsTargetingNetCoreVersionOrAbove(NuGetFramework.Parse("net6.0")))
                        {
                            Reporter.Output.WriteLine(CommonLocalizableStrings.RuntimeOptionShouldBeUsedWithSelfContained.Yellow());
                        }
                    }
                    catch
                    {
                        // Project file is not valid, continue and msbuild will error
                    }
                }
            }
        }
    }

    public enum VerbosityOptions
    {
        quiet,
        q,
        minimal,
        m,
        normal,
        n,
        detailed,
        d,
        diagnostic,
        diag
    }
}
