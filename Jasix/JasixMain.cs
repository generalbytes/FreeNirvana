using System;
using System.Collections.Generic;
using System.IO;
using CommandLine.Builders;
using CommandLine.NDesk.Options;
using Compression.Utilities;
using ErrorHandling;
using ErrorHandling.Exceptions;
using IO;
using Jasix.DataStructures;
using VariantAnnotation.Interface;

namespace Jasix;

public static class JasixMain
{
    private static          string       _inputJson;
    private static          string       _outputFile;
    private static          string       _inputJsonIndex;
    private static readonly List<string> Queries = new();
    private static          string       _section;
    private static          bool         _printHeader;
    private static          bool         _printHeaderOnly;
    private static          bool         _list;
    private static          bool         _createIndex;
    private static          string       _includeValues;


    public static int Main(string[] args)
    {
        var ops = new OptionSet
        {
            {
                "header|t",
                "print also the header lines",
                v => _printHeader = v != null
            },
            {
                "only-header|H",
                "print only the header lines",
                v => _printHeaderOnly = v != null
            },
            {
                "list|l",
                "list chromosome and section names",
                v => _list = v != null
            },
            {
                "index|c",
                "create index",
                v => _createIndex = v != null
            },
            {
                "in|i=",
                "input",
                v => _inputJson = v
            },
            {
                "in-index|d=",
                "specify an index file",
                v => _inputJsonIndex = v
            },
            {
                "include fields|f=",
                "specify a field records must include",
                v => _includeValues = v
            },
            {
                "out|o=",
                "compressed output file name (default:console)",
                v => _outputFile = v
            },
            {
                "query|q=",
                "query range",
                v => Queries.Add(v)
            },
            {
                "section|s=",
                "complete section (positions or genes) to output",
                v => _section = v
            }
        };

        ExitCodes exitCode = new ConsoleAppBuilder(args, ops)
            .Parse()
            .CheckInputFilenameExists(_inputJson, "input Json file", "[in.json.gz]")
            .DisableOutput(!_createIndex && _outputFile == null)
            .ShowBanner(Constants.Authors)
            .ShowHelpMenu("Indexes a Nirvana annotated JSON file", "-i in.json.gz [options]")
            .ShowErrors()
            .Execute(ProgramExecution);

        return (int)exitCode;
    }

    private static ExitCodes ProgramExecution()
    {
        if (_createIndex)
        {
            using (var indexCreator = new IndexCreator(_inputJson))
            {
                indexCreator.CreateIndex();
            }

            return ExitCodes.Success;
        }

        string indexFileName;

        if (_inputJsonIndex != null)
        {
            indexFileName = _inputJsonIndex;
        }
        else
        {
            indexFileName = _inputJson + JasixCommons.FileExt;
            ValidateIndexFile(indexFileName);
        }

        StreamWriter writer = string.IsNullOrEmpty(_outputFile)
            ? null
            : GZipUtilities.GetStreamWriter(_outputFile);

        string[] includeValues = string.IsNullOrEmpty(_includeValues)
            ? null
            : _includeValues.Split(',');

        using (var queryProcessor = new QueryProcessor(GZipUtilities.GetAppropriateStreamReader(_inputJson),
                   PersistentStreamUtils.GetReadStream(indexFileName), writer, includeValues))
        {
            if (_list)
            {
                queryProcessor.ListChromosomesAndSections();
                return ExitCodes.Success;
            }

            if (_printHeaderOnly)
            {
                queryProcessor.PrintHeaderOnly();
                return ExitCodes.Success;
            }

            if (!string.IsNullOrEmpty(_section))
            {
                queryProcessor.PrintSection(_section);
                return ExitCodes.Success;
            }

            if (Queries == null)
            {
                Console.WriteLine("Please specify query region(s)");
                return ExitCodes.BadArguments;
            }

            queryProcessor.ProcessQuery(Queries, _printHeader);
        }

        return ExitCodes.Success;
    }

    private static void ValidateIndexFile(string indexFileName)
    {
        if (!File.Exists(indexFileName))
            throw new UserErrorException("No index file found,please generate index file first.");
        //var indexFileCreateTime = File.GetCreationTime(indexFileName).Ticks;
        //var fileCreateTime = File.GetCreationTime(_inputJson).Ticks;
        //if (fileCreateTime > indexFileCreateTime - 1000) // adding a 100ms buffer
        //    throw new UserErrorException("Index file is older than the input file, please re-generate the index.");
    }
}