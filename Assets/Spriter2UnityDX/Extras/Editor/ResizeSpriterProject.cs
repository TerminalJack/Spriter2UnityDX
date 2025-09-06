using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.IO;

public class ResizeSpriterProject : EditorWindow
{
    public string InputPath = "";
    public string OutputPath = "";
    public float NewScale = 0.3f;

    private bool _conversionComplete;

    private static readonly float minScale = 0.05f;
    private static readonly float maxScale = 0.95f;

    [MenuItem("Assets/Resize Spriter Project...", false, 100)]
    private static void ResizeSpriterProjectMenuItem()
    {
        if (Selection.objects.Length > 0)
        {
            var obj = Selection.objects[0];
            string path = AssetDatabase.GetAssetPath(obj);

            var window = GetWindow<ResizeSpriterProject>();
            window.InputPath = path;
        }
    }

    [MenuItem("Assets/Resize Spriter Project...", true)]
    private static bool ResizeSpriterProjectMenuItem_Validate()
    {
        if (Selection.objects.Length != 1)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        return path.EndsWith(".scml", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("autosave", StringComparison.OrdinalIgnoreCase);
    }

    [MenuItem("Window/Resize Spriter Project...")]
    private static void ShowWindow()
    {
        GetWindow<ResizeSpriterProject>();
    }

    void OnEnable()
    {
        titleContent = new GUIContent("Resize Spriter Project");
        minSize = new Vector2(375, 280);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.HelpBox("Resize Spriter Project.  This utility will copy the input Spriter project's .scml " +
            "file and all of the project's image files into another folder and resize them with the scale specified by " +
            "'New Scale.'  You are strongly advised to select an empty output folder.  (Creating it, if necessary.)",
            MessageType.Info, wide: true);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Input File (.scml)  (The Spriter project to resize)");

        EditorGUILayout.BeginHorizontal();

        InputPath = EditorGUILayout.TextField(InputPath);

        if (GUILayout.Button("…", GUILayout.Width(20), GUILayout.Height(18)))
        {
            InputPath = EditorUtility.OpenFilePanel(
                title: "Select Spriter Input File",
                directory: Application.dataPath,
                extension: "scml"
            );

            if (InputPath.StartsWith(Application.dataPath))
            {
                InputPath = "Assets" + InputPath.Substring(Application.dataPath.Length);
            }

        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Output File and Folder  (The newly created project)");

        EditorGUILayout.BeginHorizontal();

        OutputPath = EditorGUILayout.TextField(OutputPath);

        if (GUILayout.Button("…", GUILayout.Width(20), GUILayout.Height(18)))
        {
            OutputPath = EditorUtility.SaveFilePanel(
                title: "Save as Spriter Project (scml & images)",
                directory: Application.dataPath,
                defaultName: "",
                extension: "scml"
            );

            if (OutputPath.StartsWith(Application.dataPath))
            {
                OutputPath = "Assets" + OutputPath.Substring(Application.dataPath.Length);

                var outputDirectory = Path.GetDirectoryName(OutputPath);

                AssetDatabase.ImportAsset(outputDirectory, ImportAssetOptions.Default); // In case the user created it.
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("The Output Project's New Scale");

        NewScale = EditorGUILayout.Slider(NewScale, minScale, maxScale);

        EditorGUILayout.Space(16);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (_conversionComplete)
        {
            if (GUILayout.Button("Dismiss", GUILayout.Width(100), GUILayout.Height(24)))
            {
                Close();
            }
        }
        else
        {
            if (GUILayout.Button("Cancel", GUILayout.Width(100), GUILayout.Height(24)))
            {
                Close();
            }

            GUILayout.FlexibleSpace();

            GUI.enabled = !string.IsNullOrEmpty(InputPath) && !string.IsNullOrEmpty(OutputPath);

            if (GUILayout.Button("Create", GUILayout.Width(100), GUILayout.Height(24)))
            {
                CreateResizedSpriterProject();
                _conversionComplete = true;
            }

            GUI.enabled = true;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void CreateResizedSpriterProject()
    {
        string lastFileHandled = "";
        int lastFileWidth = 0;
        int lastFileHeight = 0;

        var inputDirectory = Path.GetDirectoryName(InputPath);
        var outputDirectory = Path.GetDirectoryName(OutputPath);

        void ImageFileHandler(string file)
        {
            string inputFullPath = $"{inputDirectory}/{file}";
            string outputFullPath = $"{outputDirectory}/{file}";

            Debug.Log($"Resizing '{inputFullPath}' and writing to '{outputFullPath}' with a scale of {NewScale}");

            // Make sure the output directory exists.  Create it if necessary.
            string outputFileDirectory = Path.GetDirectoryName(outputFullPath);

            if (!Directory.Exists(outputFileDirectory))
            {
                Directory.CreateDirectory(outputFileDirectory);
            }

            ImageResizerUtility.ResizeImage(inputFullPath, outputFullPath, NewScale, ref lastFileWidth, ref lastFileHeight);

            lastFileHandled = file;
        }

        string GetFileWidthAttribValue(string valueStr, string file)
        {
            if (file != lastFileHandled)
            {
                Debug.LogError("GetFileWidthAttribValue(): The 'last file handled' isn't correct.  This is a programming error.");
            }

            return lastFileWidth.ToString();
        }

        string GetFileHeightAttribValue(string valueStr, string file)
        {
            if (file != lastFileHandled)
            {
                Debug.LogError("GetFileHeightAttribValue(): The 'last file handled' isn't correct.  This is a programming error.");
            }

            return lastFileHeight.ToString();
        }

        string ScaleDoubleValue(string valueStr) => (float.Parse(valueStr) * NewScale).ToString("0.######");

        var replacementsByElement = new Dictionary<string, Dictionary<string, Func<string, string, string>>>
        {
            ["file"] = new Dictionary<string, Func<string, string, string>>
            {
                ["width"] = (oldValue, file) => GetFileWidthAttribValue(oldValue, file),
                ["height"] = (oldValue, file) => GetFileHeightAttribValue(oldValue, file)
            },
            ["obj_info"] = new Dictionary<string, Func<string, string, string>>
            {
                ["w"] = (oldValue, file) => ScaleDoubleValue(oldValue),
                ["h"] = (oldValue, file) => ScaleDoubleValue(oldValue)
            },
            ["bone"] = new Dictionary<string, Func<string, string, string>>
            {
                ["x"] = (oldValue, file) => ScaleDoubleValue(oldValue),
                ["y"] = (oldValue, file) => ScaleDoubleValue(oldValue)
            },
            ["object"] = new Dictionary<string, Func<string, string, string>>
            {
                ["x"] = (oldValue, file) => ScaleDoubleValue(oldValue),
                ["y"] = (oldValue, file) => ScaleDoubleValue(oldValue)
            }
        };

        AssetDatabase.StartAssetEditing();

        UpdateSpriterFileAttributes( // And resize images.
            inPath: InputPath,
            outPath: OutputPath,
            fileHandler: (file) => ImageFileHandler(file),
            predicate: (elem, attr, val, file) => replacementsByElement.TryGetValue(elem, out var attrs) && attrs.ContainsKey(attr),
            modifier: (elem, attr, oldVal, file) => replacementsByElement[elem][attr](oldVal, file)
        );

        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static void UpdateSpriterFileAttributes(
        string inPath,
        string outPath,
        Action<string> fileHandler,
        Func<string, string, string, string, bool> predicate,
        Func<string, string, string, string, string> modifier)
    {
        var readerSettings = new XmlReaderSettings
        {
            IgnoreWhitespace = false,
            IgnoreComments = false,
            IgnoreProcessingInstructions = false
        };

        var writerSettings = new XmlWriterSettings
        {
            Indent = false,
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = false
        };

        using (var reader = XmlReader.Create(inPath, readerSettings))
        using (var writer = XmlWriter.Create(outPath, writerSettings))
        {
            writer.WriteStartDocument(true);

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        string elementName = reader.LocalName;
                        string currentFileName = elementName == "file" ? reader.GetAttribute("name") : null;

                        if (!string.IsNullOrEmpty(currentFileName))
                        {
                            fileHandler(currentFileName);
                        }

                        // write the <tag>
                        writer.WriteStartElement(
                            reader.Prefix,
                            reader.LocalName,
                            reader.NamespaceURI);

                        // copy/patch each attribute
                        if (reader.HasAttributes)
                        {
                            reader.MoveToFirstAttribute();

                            do
                            {
                                var attribName = reader.Name;
                                var attribValue = reader.Value;

                                if (predicate(elementName, attribName, attribValue, currentFileName))
                                {
                                    attribValue = modifier(elementName, attribName, attribValue, currentFileName);
                                }

                                writer.WriteAttributeString(
                                    reader.Prefix,
                                    reader.LocalName,
                                    reader.NamespaceURI,
                                    attribValue);
                            }
                            while (reader.MoveToNextAttribute());

                            reader.MoveToElement();
                        }

                        // automatically close empty elements
                        if (reader.IsEmptyElement)
                        {
                            writer.WriteEndElement();
                        }

                        break;

                    case XmlNodeType.EndElement:
                        writer.WriteFullEndElement();
                        break;

                    case XmlNodeType.Text:
                        writer.WriteString(reader.Value);
                        break;

                    case XmlNodeType.CDATA:
                        writer.WriteCData(reader.Value);
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        writer.WriteProcessingInstruction(
                            reader.Name,
                            reader.Value);
                        break;

                    case XmlNodeType.Comment:
                        writer.WriteComment(reader.Value);
                        break;

                    case XmlNodeType.DocumentType:
                        writer.WriteDocType(
                            reader.Name,
                            reader.GetAttribute("PUBLIC"),
                            reader.GetAttribute("SYSTEM"),
                            reader.Value);
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        // Preserve indent/newlines
                        writer.WriteWhitespace(reader.Value);
                        break;

                    default:
                        // writer.WriteNode(reader, false);
                        break;
                }
            }
        }
    }

    private static class ImageResizerUtility
    {
        public static bool ResizeImage(string inputPath, string outputPath, float scale,
            ref int newWidth, ref int newHeight)
        {
            if (!File.Exists(inputPath))
            {
                Debug.LogError($"ResizeImage: Input file not found: {inputPath}");
                return false;
            }

            Texture2D source = LoadTexture(inputPath);
            if (source == null)
            {
                Debug.LogError("ResizeImage: Failed to load texture.");
                return false;
            }

            source.wrapMode   = TextureWrapMode.Clamp;
            source.wrapModeU  = TextureWrapMode.Clamp;
            source.wrapModeV  = TextureWrapMode.Clamp;

            newWidth = Mathf.FloorToInt(source.width * scale + 0.5f);
            newHeight = Mathf.FloorToInt(source.height * scale + 0.5f);

            BleedTransparentBorderRadius(source, radius: 4);

            Texture2D resized = PreBlurAndResize(source, newWidth, newHeight);

            SaveTexture(resized, outputPath);

            DestroyImmediate(source);
            DestroyImmediate(resized);

            return true;
        }

        /// Fills transparent pixels by averaging neighbors within 'radius'.
        static void BleedTransparentBorderRadius(Texture2D tex, int radius)
        {
            int w = tex.width;
            int h = tex.height;

            Color[] src = tex.GetPixels();
            Color[] dst = new Color[src.Length];

            // Precompute neighbor offsets
            var offsets = new List<Vector2Int>();

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        offsets.Add(new Vector2Int(dx, dy));
                    }
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color c = src[i];
                    if (c.a > 0f)
                    {
                        dst[i] = c;
                        continue;
                    }

                    Vector3 colSum = Vector3.zero;
                    float  aSum   = 0f;

                    foreach (var off in offsets)
                    {
                        int nx = x + off.x, ny = y + off.y;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h)
                        {
                            continue;
                        }

                        Color n = src[ny * w + nx];
                        if (n.a > 0f)
                        {
                            colSum += new Vector3(n.r * n.a, n.g * n.a, n.b * n.a);
                            aSum += n.a;
                        }
                    }

                    if (aSum > 0f)
                    {
                        var avg = colSum / aSum;
                        dst[i] = new Color(avg.x, avg.y, avg.z, 0f);
                    }
                    else
                    {
                        dst[i] = c;
                    }
                }
            }

            tex.SetPixels(dst);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        }

        private static Texture2D LoadTexture(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            tex.LoadImage(data, false); // Keep readable

            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();

            return tex;
        }

        private static Texture2D PreBlurAndResize(Texture2D source, int outW, int outH)
        {
            // Prepare descriptors
            var desc = new RenderTextureDescriptor(source.width, source.height,
                RenderTextureFormat.ARGB32, 0)
            {
                sRGB            = QualitySettings.activeColorSpace == ColorSpace.Linear,
                useMipMap       = false,
                autoGenerateMips= false
            };

            // When creating RTs:
            desc.colorFormat   = RenderTextureFormat.ARGB32;
            desc.useMipMap     = false;
            desc.autoGenerateMips = false;

            // Create two temp RTs for blur
            var rtH = RenderTexture.GetTemporary(desc);
            rtH.wrapMode = TextureWrapMode.Clamp;
            var rtV = RenderTexture.GetTemporary(desc);
            rtV.wrapMode = TextureWrapMode.Clamp;

            var blurMat = new Material(Shader.Find("Hidden/SeparableGaussianBlur"));

            // Shader expects _Direction = (1,0) or (0,1)
            blurMat.SetVector("_Direction", Vector2.right);
            Graphics.Blit(source, rtH, blurMat);

            blurMat.SetVector("_Direction", Vector2.up);
            Graphics.Blit(rtH, rtV, blurMat);

            // Now bicubic-resize the blurred RT into final RT
            desc.width  = outW;
            desc.height = outH;
            var rtFinal = RenderTexture.GetTemporary(desc);

            var bicubicMat = new Material(Shader.Find("Hidden/BicubicResize"));
            Graphics.Blit(rtV, rtFinal, bicubicMat);

            // Read back to Texture2D
            RenderTexture.active = rtFinal;
            var result = new Texture2D(outW, outH,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: QualitySettings.activeColorSpace == ColorSpace.Linear);
            result.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
            result.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            // Cleanup
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rtH);
            RenderTexture.ReleaseTemporary(rtV);
            RenderTexture.ReleaseTemporary(rtFinal);

            DestroyImmediate(blurMat);
            DestroyImmediate(bicubicMat);

            return result;
        }

        private static void SaveTexture(Texture2D tex, string outputPath)
        {
            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(outputPath, pngData);
            AssetDatabase.Refresh();
        }
    }
}
