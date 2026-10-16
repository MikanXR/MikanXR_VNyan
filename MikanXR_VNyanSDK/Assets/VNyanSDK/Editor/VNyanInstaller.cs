using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

public class VNyanInstaller : EditorWindow
{
    private static Texture2D vnyanIcon;


    [MenuItem("VNyan SDK/Additional Package Installer")]
    public static void ShowWindow()
    {
        OpenWindow();
    }


    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        if (File.Exists("vnyansdk_init.txt"))
        {
            return;
        }

        File.WriteAllText("vnyansdk_init.txt", "This file is used to track whether the VNyan SDK installer has been shown before. You can delete this file to show the installer again.");
        OpenWindow();
    }

    private static void OpenWindow()
    {

        vnyanIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VNyanSDK/Textures/vnyan.png");
        var window = GetWindow<VNyanInstaller>("VNyan SDK");
        window.minSize = new Vector2(360, 300);


    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal("box");
        {
            GUILayout.Label(vnyanIcon, GUILayout.Width(48), GUILayout.Height(48));
            GUILayout.Space(12);

            GUILayout.BeginVertical();
            GUILayout.Label("VNyan SDK", EditorStyles.boldLabel);
            GUILayout.Label("Welcome to VNyanSDK. Would you like to install any optional features?", EditorStyles.wordWrappedLabel);
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();

        ShowOption("VNyan Avatar SDK", "VNyan Avatar SDK provides a handful of checks and features to prepare your model for export as a VNyan Avatar\n\nDownloads and installs UniVRM 0.104 prerequisite", "Install", () => { InstallAvatarSDK(); });

        ShowOption("VSeeFace Stubs", "Component stubs from VSeeFace SDK. This allows usage of VSeeFace SDK components in VNyan assets and Avatars\n\nRecommended instead of installing full VSeeFace SDK", "Install", () => { InstallVSFStubs(); });

    }



    private void ShowOption(string title, string body, string buttonText, Action buttonAction)
    {
        GUILayout.BeginHorizontal("box");

        //GUILayout.BeginVertical(GUILayout.Width(36));
        //GUILayout.Label(icon, GUILayout.Width(36), GUILayout.Height(36));
        //GUILayout.EndVertical();

        GUILayout.Space(8);
        GUILayout.BeginVertical();
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Label(body, EditorStyles.wordWrappedLabel);
        GUILayout.EndVertical();
        GUILayout.Space(8);

        GUILayout.BeginVertical(GUILayout.Width(60));
        if (GUILayout.Button(buttonText))
            buttonAction();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private static void InstallVSFStubs()
    {
        UnityEditor.AssetDatabase.ImportPackage("Assets/VNyanSDK/Optional/VSFStubs.unitypackage", false);
    }

    private static void InstallAvatarSDK()
    {
        InstallUniVRM();

        UnityEditor.AssetDatabase.ImportPackage("Assets/VNyanSDK/Optional/AvatarSDK.unitypackage", false);
    }

    private static void InstallUniVRM()
    {
        downloadPackage("https://github.com/vrm-c/UniVRM/releases/download/v0.104.0/UniVRM-0.104.0_c466.unitypackage", "UniVRM-0.104.0_c466.unitypackage");
    }

    private static void downloadPackage(string uri, string localFile)
    {

        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
        request.Method = "GET";

        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if ((int)response.StatusCode == 200)
                {
                    long _totalBytesLength = response.ContentLength;

                    using (Stream responseStream = response.GetResponseStream())
                    using (FileStream fileStream = File.Create(localFile))
                    {
                        byte[] buffer = new byte[1024 * 1024]; 

                        int size = responseStream.Read(buffer, 0, buffer.Length);
                        while (size > 0)
                        {
                            fileStream.Write(buffer, 0, size);

                            size = responseStream.Read(buffer, 0, buffer.Length);
                        }
                    }

                    AssetDatabase.ImportPackage(localFile, false);

                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }


    }

}
