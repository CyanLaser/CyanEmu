using UnityEngine;
using System.IO;
using UnityEditor;
using System;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuFileModifier
    {
        const string MODIFICATIONS_FILE_PATH = "Assets/CyanEmu/Resources/FileModifications/modifications.txt";
        const string VRCSDK_PATH = "Assets/VRCSDK";

        const string ADD_AFTER_OPERATION = "AddAfter";
        const string DELETE_OPERATION = "Delete";
        const string REPLACE_OPERATION = "Replace";

        static readonly char[] WHITE_SPACE = { ' ' };


        [MenuItem("Window/CyanEmu/Apply SDK Modifications", priority = 1020)]
        public static void PerformModifications()
        {
            if (!EditorUtility.DisplayDialog("Apply SDK Modifications", "Would you like to modify the VRCSDK to include scrollbars for the build panel and an \"Execute\" button on VRC_Triggers?", "Modify SDK", "Cancel"))
            {
                return;
            }

            FileInfo modificationsFile = new FileInfo(MODIFICATIONS_FILE_PATH);
            if (!modificationsFile.Exists)
            {
                Debug.LogError("Modifications file does not exist!");
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(MODIFICATIONS_FILE_PATH))
                {
                    int numFiles = int.Parse(reader.ReadLine());

                    for (int curFile = 0; curFile < numFiles; ++curFile)
                    {
                        string filename = reader.ReadLine();
                        Debug.Log("Attempting to modify " + filename);

                        FileInfo file = GetFile(filename);
                        bool performOperations = true;
                        if (file == null || !file.Exists)
                        {
                            Debug.LogWarning("File to modify does not exist! " + filename);
                            performOperations = false;
                        }

                        string fileContents = performOperations 
                            ? File.ReadAllText(file.FullName).Replace("\r\n", "\n") 
                            : "";

                        int numOperations = int.Parse(reader.ReadLine());

                        for (int curOp = 0; curOp < numOperations; ++curOp)
                        {
                            string[] opLine = reader.ReadLine().Split(WHITE_SPACE, StringSplitOptions.RemoveEmptyEntries);

                            string op = opLine[0];

                            if (op == ADD_AFTER_OPERATION)
                            {
                                int numSearchLines = int.Parse(opLine[1]);
                                int numReplaceLines = int.Parse(opLine[2]);

                                string searchLines = "";
                                for (int curLine = 0; curLine < numSearchLines; ++curLine)
                                {
                                    if (curLine != 0)
                                    {
                                        searchLines += "\n";
                                    }
                                    searchLines += reader.ReadLine();
                                }

                                string replace = searchLines;
                                for (int curLine = 0; curLine < numReplaceLines; ++curLine)
                                {
                                    replace += "\n" + reader.ReadLine();
                                }

                                if (!performOperations)
                                {
                                    continue;
                                }
                                
                                if (fileContents.Contains(replace))
                                {
                                    Debug.LogWarning("File [" + filename + "] already contains added lines. Skipping");
                                    continue;
                                }

                                int index = fileContents.IndexOf(searchLines);
                                if (fileContents.IndexOf(searchLines, index + 1) != -1)
                                {
                                    Debug.LogWarning("File [" + filename + "] contains multiple copies of the lines to replace. Skipping");
                                    continue;
                                }

                                fileContents = fileContents.Replace(searchLines, replace);
                                Debug.Log("Replacing lines:\n" + replace);
                            }
                            else if (op == DELETE_OPERATION)
                            {
                                int numSearchLines = int.Parse(opLine[1]);

                                string searchLines = "";
                                for (int curLine = 0; curLine < numSearchLines; ++curLine)
                                {
                                    if (curLine != 0)
                                    {
                                        searchLines += "\n";
                                    }
                                    searchLines += reader.ReadLine();
                                }
                                
                                if (!performOperations)
                                {
                                    continue;
                                }

                                if (!fileContents.Contains(searchLines))
                                {
                                    Debug.LogWarning("File [" + filename + "] does not contain lines to delete. Skipping");
                                    continue;
                                }

                                int index = fileContents.IndexOf(searchLines);
                                if (fileContents.IndexOf(searchLines, index + 1) != -1)
                                {
                                    Debug.LogWarning("File [" + filename + "] contains multilpe copies of the lines to Delete. Skipping");
                                    continue;
                                }

                                fileContents = fileContents.Replace(searchLines, "");
                                Debug.Log("Deleting lines:\n" + searchLines);
                            }
                            else if (op == REPLACE_OPERATION)
                            {
                                // TODO
                            }
                        }

                        if (!performOperations)
                        {
                            continue;
                        }
                        
                        File.WriteAllText(file.FullName, fileContents);
                    }
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to modify files!\n" + e.StackTrace);
            }
        }

        private static FileInfo GetFile(string filename)
        {
            DirectoryInfo sdkDir = new DirectoryInfo(VRCSDK_PATH);
            FileInfo[] fileInfos = sdkDir.GetFiles(filename, SearchOption.AllDirectories);

            if (fileInfos.Length > 0)
            {
                return fileInfos[0];
            }
            return null;
        }
    }
}