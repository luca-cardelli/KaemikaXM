using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using CalithaGoldParser;

namespace KaemikaAssets 
{

    public class AssetError : Exception {
        public AssetError(string message) : base(message) { }
    }

    public static class SharedAssets
    {

        public static List<string> symbols = new List<string> {
                "∂", "μ", "σ", "±", "ʃ", "√", "∑", "∏", "\'",                                                  // Symbols
                "⁺", "⁻", "⁼", "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹", "⁽", "⁾",                     // Superscript
                "_", "₊", "₋", "₌", "₀", "₁", "₂", "₃", "₄", "₅", "₆", "₇", "₈", "₉", "₍", "₎"};               // Subscript

        // LOADING FILES EMBEDDED AS RESOURCES
        // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/data-cloud/data/files?tabs=windows#Loading_Files_Embedded_as_Resources

        public static string TextAsset(string filename) {
            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(SharedAssets)).Assembly;
            // "SharedAssets" is any class in the package/assembly where the assets are stored, to get at the assembly
            // in this case we are storing them in the same package/assembly as this SharedAssets class
            // inside an Assets subfolder

            Stream assetStream = assembly.GetManifestResourceStream("KaemikaAssets.Assets." + filename);
            if (assetStream == null) throw new AssetError("TextAsset file not found: " + filename);
            // "KaemikaAssets" is the name of a namespace, it can be from a shared package
            // "Assets" (if used) is the name of a directory in that package
            // "filename" is the name of a file in that directory.

            // That file should have a VisualStudio property of "Build Action=Embedded resource".

            string text = "";
            try {
                using (var rdr = new System.IO.StreamReader(assetStream)) {
                    text = rdr.ReadToEnd();
                }
            } catch (Exception) { throw new AssetError("TextAsset resource not read: " + filename); }

            return text;
        }
        
        public static LALRParser GoldParser(string filename) {
            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(SharedAssets)).Assembly;
            Stream assetStream = assembly.GetManifestResourceStream("KaemikaAssets.Assets." + filename);
            if (assetStream == null) throw new AssetError("GoldParser file not found: " + filename);
            try {
                MemoryStream cgtStream = new MemoryStream();
                assetStream.CopyTo(cgtStream);  // copy Stream to a MemoryStream because CGTReader will try to ask for the length of Stream, and a plain Stream does not suport that
                cgtStream.Position = 0;         // after CopyTo the memorystream position is at the end, so reset it
                //numberFormatInfo = new NumberFormatInfo();
                //numberFormatInfo.NumberDecimalSeparator = ".";
                CGTReader reader = new CGTReader(cgtStream);
                LALRParser parser = reader.CreateNewParser();
                parser.TrimReductions = false;
                parser.StoreTokens = LALRParser.StoreTokensMode.NoUserObject;
                return parser;
            } catch (Exception ex) {
                throw new AssetError("Parser loading failed: " + ex.Message);
            }
        }

    }
}
