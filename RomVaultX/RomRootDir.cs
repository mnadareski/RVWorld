using System;
using RomVaultX.Util;

namespace RomVaultX
{
    public static class RomRootDir
    {
        private static readonly string[] rootDirs;

        static RomRootDir()
        {
            rootDirs = new string[256];
            for (int i = 0; i < 256; i++)
                rootDirs[i] = @"RomRoot\";

            if (!System.IO.File.Exists("DirPaths.conf"))
                return;

            string text = System.IO.File.ReadAllText(@"DirPaths.conf");

            text = text.Replace("\r", "");
            string[] rules = text.Split('\n');

            foreach (string rule in rules)
            {
                if (rule.Length < 6)
                    continue;

                string pStart = rule.Substring(0, 2);
                string ps0 = rule.Substring(2, 1);
                string pEnd = rule.Substring(3, 2);
                string ps1 = rule.Substring(5, 1);
                string pDir = rule.Substring(6);
                if (ps0 != "-")
                    continue;
                if (ps1 != "|")
                    continue;

                int iStart = Convert.ToInt32(pStart, 16);
                int iEnd = Convert.ToInt32(pEnd, 16);

                for (int i = iStart; i <= iEnd; i++)
                    rootDirs[i] = pDir + @"\";
            }
        }
        
        public static string GetRootDir(byte b0)
        {
            return rootDirs[b0];
        }

        public static string Getfilename(byte[] sha1)
        {
            if (!int.TryParse(AppSettings.ReadSetting("Depth"), out int depth))
                depth = 2;

            if (depth < 0)
                return null;

            string path = GetRootDir(sha1[0]);
            for (int i = 0; i < depth; i++)
                path += VarFix.ToString(sha1[i]) + @"\";

            return path + VarFix.ToString(sha1) + ".gz";
        }
    }
}
