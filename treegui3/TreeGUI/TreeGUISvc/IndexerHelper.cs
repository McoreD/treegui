﻿using ShareX.HelpersLib;
using ShareX.IndexerLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeGUI
{
    public static class IndexerHelper
    {
        public static string GetIndexFilePath(Config config, string dirPath)
        {
            string indexDir = config.OutputMode == OutputMode.CustomDirectory ? config.CustomDirectory : dirPath;

            string fileName = Helpers.GetValidFileName($"{indexDir} {config.FileName}.{config.IndexerSettings.Output.ToString().ToLower()}", " ");

            if (config.PrependDate)
                fileName = $"{DateTime.Now.ToString("yyyy-MM-dd")} {fileName}";

            if (Directory.Exists(indexDir))
            {
                return Path.Combine(indexDir, fileName);
            }

            return null;
        }

        public static void Index(Config config)
        {
            config.IndexerSettings.BinaryUnits = true;

            config.Folders.ForEach(dirPath =>
            {
                string index = Indexer.Index(dirPath, config.IndexerSettings);
                if (!string.IsNullOrEmpty(index))
                {
                    string filePath = GetIndexFilePath(config, dirPath);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        using (StreamWriter sw = new StreamWriter(filePath))
                        {
                            sw.Write(index);
                        }
                    }
                }
            });
        }
    }
}