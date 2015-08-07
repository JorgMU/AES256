using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AES265
{
  class Program
  {
    //readonly
    private static readonly char[] _cmdChars = new char[] { '-', '/' };
    private static readonly string _exePath = Environment.GetCommandLineArgs()[0];
    private static readonly string _exeName = Path.GetFileName(_exePath);

    static void Main(string[] args)
    {

      if (args.Length < 1) ShowUse(true);

      string buffer = "";
      string opt = "";
      string pass = "";
      string inFile = "";  //use STDIN if empty
      string outFile = ""; //use STDOUT if empty

      foreach (string s in args)
      {

        string o = s.TrimStart(_cmdChars);
        if(o.Length > 1) o = o.Substring(0,1);
        int i = s.IndexOf(':');
        string d = (i > 0) ? s.Substring(i+1) : "";

        //Console.Error.WriteLine("[{0}] [{1}] [{2}]", s, o, d); //debug

        switch (o)
        {
          case "h":
          case "?": ShowUse(true); break;
          case "e":
            if (opt != "") Error("Only one operation can be specified");
            opt = "e";
            break;
          case "d":
            if (opt != "") Error("Only one operation can be specified");
            opt = "d";
            break;
          case "p":
            if (pass != "") Error("Only one passphrase can be specified");
            pass = d;
            break;
          case "i":
            if (inFile != "") Error("Only one input file can be specified");
            if (buffer != "") Error("Only one input method can be specified, -i cannot be used with -s");
            inFile = d;
            break;
          case "o":
            if (outFile != "") Error("Only one output file can be specified");
            outFile = d;
            break;
          case "s":
            if (inFile != "") Error("Only one input method can be specified, -i cannot be used with -s");
            buffer = d;
            break;
          default: Error("*Unrecognized option: {0}", s);
            break;
        }
      }

      if (pass == "") Error("Passphrase is always required");

      FileInfo ifi = null;
      FileInfo ofi = null;

      if (inFile != "")
      {
        try { ifi = new FileInfo(inFile); }
        catch (SystemException se) { Error(se, "*Invalid input file: {0}", inFile); }

        if (!ifi.Exists) Error("*Input file does not exist: {0}", inFile);
      }

      if (outFile != "")
      {
        try { ofi = new FileInfo(outFile); }
        catch (SystemException se)
        {
          Error(se, "Invalid Output file: {0}", inFile);
        }
        string outDir = ofi.DirectoryName;
        if (!Directory.Exists(outDir))
          Error("*Output directory does not exits: {0}", outDir);
      }

      if (buffer == "") {
        if(ifi != null && File.Exists(inFile))
        {
          buffer = File.ReadAllText(inFile);
        }
        else
        {
          StreamReader sw = null;
          try { sw = new StreamReader(Console.OpenStandardInput()); }
          catch { };
          if (sw == null) Error("Could not read from STDIN");
          buffer = sw.ReadToEnd().TrimEnd();
          sw.Close();
        }
      }

      string result = "";

      if (opt == "d")
      {
        try { result = SimpleAES256.DecryptText(buffer, pass); }
        catch (SystemException se) { Error(se, "*Error during decryption"); }
      }
      else
      {
        try { result = SimpleAES256.EncryptText(buffer, pass); }
        catch (SystemException se) { Error(se, "*Error during encryption"); }
      }

      if (ofi != null)
      {
        StreamWriter sw = null;
        try
        {
          sw = new StreamWriter(ofi.OpenWrite());
          sw.Write(result);
          sw.Close();
        }
        catch (SystemException se)
        {
          Error(se, "*Error writing output file");
        }
        finally
        {
          if (sw != null) sw.Close();
        }
      }
      else Console.Write(result);

    }

    private static void Error(string Message, params object[] Items)
    {
      Console.Error.WriteLine(Message, Items);
      Environment.Exit(-1);
    }

    private static void Error(SystemException se, string Message, params object[] Items)
    {
      Console.Error.WriteLine(se.Message);
      if (se.InnerException != null) Console.Error.WriteLine(se.InnerException.Message);
      Error(Message, Items);
    }

    private static void ShowUse(bool ExitProgram)
    {
      ShowUse();
      if(ExitProgram) Environment.Exit(0);
    }

    private static void ShowUse()
    {
      Console.Error.WriteLine(@" AES256 v1.0 - jorgie@missouri.edu ({0})

 This program does simple AES256 encryption and base64 encoding of the data
 passed in via STDIN or read from a text file, using the password passed in
 on the command line.

  Use: {0} -p:passphrase [-e|-d] [-i:in.txt|-s:""Some plain text""] [-o:out.txt]

  -e Encrypt (default)
  -d Decrypt
  -p Passphrase
  -i Input text file name
  -s Input string - careful the commandline can mess with things
  -o Ouput text file name
     If -i and -s are omitted, STDIN is used
     If -o is omitted, STDOUT is used
  -The input text is treated at UTF8", _exeName);

    }
  }
}
