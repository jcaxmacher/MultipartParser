# MultipartParser

A basic (very basic) parser for multipart responses.

## Installation

The assembly for .NET 4 can be installed from NuGet.  Building this project
from source requires the NuGet extension for Visual Studio so that the
following two dependencies are available at build time:

*  Sprache
*  NUnit

## Usage

    using MultipartParser;
    using MultipartParser.Data;

    namespace Test
    {
        class Test
        {
            static void Main(string[] args)
            {
                // Http response content type
                string contentTypeData = "multipart/related; boundary=XXX; type=text/xml";
                string testData = @"... long test data with actual http response";
    
                // Parse from a string
                List<Message> messages = Parser.ParseMessages(contentTypeData, testData);

                Stream httpResponseStream = ...

                // Or parse from a stream
                List<Message> messages = Parser.ParseMessags(contentTypeData, httpResponseStream);
            }
        }
    }


