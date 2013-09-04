using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprache;
using System.IO;

namespace MultipartParser
{
    public class Parser
    {
        static string controlChars =
            new String(Enumerable.Range(0, 31).Select(x => Convert.ToChar(x)).ToArray());
        static string delChar = ((char)127).ToString();
        static string spaceChar = " ";
        static string tSpecialChars = "()<>@,;:\\\"/[]?.=";
        static string allSpecialChars =
            controlChars + delChar + spaceChar + tSpecialChars;
        static string doubleDash = "--";

        static Parser<char> NonSpecialChar = Parse.CharExcept(allSpecialChars);
        static Parser<char> NonSemicolonChar = Parse.CharExcept("\";\r\n");
        internal static Parser<string> TokenString = NonSpecialChar.AtLeastOnce().Text();
        internal static Parser<string> SemiTokenString = NonSemicolonChar.AtLeastOnce().Text();

        static Parser<char> Quote = Parse.Char('"');
        static Parser<char> EscapedQuote = Parse.Char('\\').Then(_ => Parse.Char('"'));
        static Parser<char> NonQuote = Parse.CharExcept('"');
        static Parser<char> MediaSeparator = Parse.Char('/');
        static Parser<char> AttributeSeparator = Parse.Char(';');
        static Parser<char> AttributeValueSeparator = Parse.Char('=');
        static Parser<char> HeaderSeparator = Parse.Char(':');
        static Parser<IEnumerable<char>> NewLine = Parse.String("\r\n");
        static Parser<IEnumerable<char>> DoubleNewLine = Parse.String("\r\n\r\n");
        static Parser<IEnumerable<char>> DoubleDash = Parse.String(doubleDash);

        static Parser<char> QuotedStringChar = EscapedQuote.Or(NonQuote);
        static Parser<string> QuotedString = QuotedStringChar.Many().Text();

        internal static Parser<string> InnerString =
            from startQuote in Quote
            from innerString in QuotedString
            from endQuote in Quote
            select innerString;

        static Parser<string> TokenOrInnerString = SemiTokenString.Or(InnerString);

        internal static Parser<Tuple<string, string>> MediaType =
            from mediaType in TokenString
            from divider in MediaSeparator
            from mediaSubType in TokenString
            select Tuple.Create(mediaType, mediaSubType);

        internal static Parser<Tuple<string, string>> MediaAttribute =
            from semicolon in AttributeSeparator
            from attr in TokenString.Token()
            from equalSign in AttributeValueSeparator
            from value in TokenOrInnerString.Token()
            select Tuple.Create(attr, value);

        static Parser<Dictionary<string, string>> MediaAttributes =
            from attributes in MediaAttribute.Many()
            select attributes.ToDictionary(t => t.Item1, t => t.Item2);

        public static Parser<Data.ContentType> ContentType =
            from mediaType in MediaType
            from attrs in MediaAttributes.End()
            select new Data.ContentType(mediaType: mediaType.Item1, mediaSubType: mediaType.Item2, attrs: attrs);
                static Parser<string> MessagePartStart(string boundary)
        {
            boundary = doubleDash + boundary;
            return Parse.String(boundary).Text();
        }

        static Parser<string> MessageBoundary(string boundary)
        {
            return NewLine.Many().Then(_ => MessagePartStart(boundary));
        }

        static Parser<object> StartMessageBoundary(string boundary)
        {
            return MessageBoundary(boundary).Then(_ => Parse.Not(DoubleDash));
        }

        static Parser<object> EndMessageBoundary(string boundary)
        {
            return MessageBoundary(boundary).Then(_ => DoubleDash).Token();
        }

        static Parser<string> MessageBody(string boundary)
        {
            return Parse.AnyChar.Except(MessageBoundary(boundary)).Many().Text();
        }

        static Parser<string> HeaderName = TokenString.Token();
        static Parser<string> HeaderValue = Parse.AnyChar.Except(NewLine).Many().Text().Token();
            
        static Parser<Tuple<string, string>> Header =
            from headerName in HeaderName
            from separator in HeaderSeparator
            from headerValue in HeaderValue
            select Tuple.Create(headerName, headerValue);

        static Parser<Dictionary<string, string>> Headers =
            from headers in Header.Except(DoubleNewLine).Many()
            select headers.ToDictionary(h => h.Item1, h => h.Item2);

        static Parser<Data.Message> Message(string boundary)
        {
            return from startBoundary in StartMessageBoundary(boundary)
                   from headers in Headers
                   from body in MessageBody(boundary)
                   select new Data.Message(headers: headers, body: body);
        }

        public static Sprache.Parser<List<Data.Message>> Messages(string boundary)
        {
            return from messages in Message(boundary).Many()
                   from endMessage in EndMessageBoundary(boundary)
                   select messages.ToList();
        }

        public static List<Data.Message> ParseMessages(string contentTypeRawText, string responseBody)
        {
            Data.ContentType contentType = Parser.ContentType.End().Parse(contentTypeRawText);
            return Parser.Messages(contentType["boundary"]).Parse(responseBody);
        }

        public static List<Data.Message> ParseMessages(string contentTypeRawText, Stream responseBody)
        {
            var reader = new StreamReader(responseBody, System.Text.Encoding.GetEncoding("utf-8"));
            return ParseMessages(contentTypeRawText, reader.ReadToEnd());
        }
    }
}
