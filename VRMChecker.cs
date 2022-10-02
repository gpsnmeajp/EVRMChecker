using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using VRM;
using UniVRM10;

public class VRMChecker : MonoBehaviour
{
    public GameObject Model = null;
    public GameObject Model10 = null;
    public bool ModelChenged = false;
    public bool Model10Chenged = false;
    public Text VRM0_Text;
    public Text VRM1_Text;
    public Text VRM0_Info;
    public Text VRM1_Info;
    public string vrmfilepath;
    SynchronizationContext synchronizationContext;

    string migrationMessage = "";

    void Start()
    {
        synchronizationContext = SynchronizationContext.Current;
        Screen.SetResolution(1280, 768, false, 60);
    }

    void Update()
    {
        if (Model10)
        {
            Model10.transform.position = new Vector3(-1, (float)Math.Cos(Time.time*10)*0.05f, 0);

            if (Model10Chenged)
            {
                Model10Chenged = false;
                string expressionList = "ExpressionKeys\n";
                foreach (var E in Model10.GetComponent<Vrm10Instance>().Runtime.Expression.ExpressionKeys)
                {
                    expressionList += E.ToString() + "\n";
                }
                Debug.Log(expressionList);
                VRM1_Info.text = expressionList;
            }
        }
        if (Model)
        {
            Model.transform.position = new Vector3(1, (float)Math.Cos(Time.time*10)* 0.05f, 0);

            if (ModelChenged)
            {
                ModelChenged = false;
                string blendList = "VRMBlendShapeProxy\n";
                foreach (var E in Model.GetComponent<VRMBlendShapeProxy>().GetValues())
                {
                    blendList += E.Key.ToString() + "\n";
                }
                Debug.Log(blendList);
                VRM0_Info.text = blendList;
            }
        }
    }

    private void OnGUI()
    {
        var ButtonStyle = new GUIStyle(GUI.skin.button);
        ButtonStyle.fontSize = 24;
        var TextFieldStyle = new GUIStyle(GUI.skin.textField);
        TextFieldStyle.fontSize = 24;
        var LabelStyle = new GUIStyle(GUI.skin.label);
        LabelStyle.fontSize = 24;

        if (GUILayout.Button("Load VRM", ButtonStyle))
        {
            // Open file with filter
            var extensions = new[] {
                new ExtensionFilter("VRM Model", "vrm" ),
                new ExtensionFilter("All Files", "*" )
            };
            var path = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            if (path.Length > 0) {
                LoadVRM(path[0]);
                LoadVRM10(path[0]);
            }            
        }
    }


    //Load VRM file on runtime
    public void LoadVRM10(string path)
    {
        if (Model10 != null)
        {
            Destroy(Model10);
            Model10 = null;
        }

        if (File.Exists(path))
        {
            VRM1_Text.text = "VRM1\nLoading...";
            VRM1_Info.text = "";

            vrmfilepath = path;
            byte[] VRMdataRaw = File.ReadAllBytes(path);
            LoadVRM10FromData(VRMdataRaw);
        }
        else
        {
            VRM1_Text.text = "VRM1\nFile not found";
            Debug.LogError("File not found: " + path);
        }
    }

    //Load VRM data on runtime
    //You can receive VRM over the network or file or other.
    public void LoadVRM10FromData(byte[] VRMdataRaw)
    {
        try
        {
            migrationMessage = "";
            UniGLTF.GlbLowLevelParser glbLowLevelParser = new UniGLTF.GlbLowLevelParser(null, VRMdataRaw);
            UniGLTF.GltfData gltfData = glbLowLevelParser.Parse();
            UniVRM10.Vrm10Data vrm = UniVRM10.Vrm10Data.Parse(gltfData);
            UniGLTF.GltfData migratedGltfData = null;

            if (vrm == null)
            {
                //Auto migration
                UniVRM10.MigrationData mdata;
                migratedGltfData = UniVRM10.Vrm10Data.Migrate(gltfData, out vrm, out mdata);
                migrationMessage = mdata.Message;
                if (vrm == null)
                {
                    VRM1_Text.text = "VRM1\nLoad Error: " + mdata.Message;
                    Debug.LogError(mdata.Message);
                    return;
                }
            }

            UniVRM10.Vrm10Importer vrmImporter = new UniVRM10.Vrm10Importer(vrm);

            synchronizationContext.Post(async (_) => {
                try
                {
                    UniGLTF.RuntimeGltfInstance gltfInstance = await vrmImporter.LoadAsync(new VRMShaders.ImmediateCaller());
                    gltfData.Dispose();
                    vrmImporter.Dispose();

                    Model10 = gltfInstance.Root;
                    Model10.transform.parent = this.transform;

                    gltfInstance.EnableUpdateWhenOffscreen();
                    gltfInstance.ShowMeshes();

                    VRM1_Text.text = "VRM1\nLoad OK";
                    if (migratedGltfData != null)
                    {
                        VRM1_Text.text = "VRM1\nLoad OK("+ migrationMessage+ ")";
                        migratedGltfData.Dispose();
                    }
                    Model10Chenged = true;
                }
                catch (Exception e)
                {
                    VRM1_Text.text = "VRM1\nLoad Error: " + e.Message;
                }
            }, null);
        }
        catch (Exception e)
        {
            VRM1_Text.text = "VRM1\nLoad Error: " + e.Message;
        }
    }


    //Load VRM file on runtime
    public void LoadVRM(string path)
    {
        if (Model != null)
        {
            Destroy(Model);
            Model = null;
        }

        if (File.Exists(path))
        {
            VRM0_Text.text = "VRM0\nLoading...";
            VRM0_Info.text = "";
            vrmfilepath = path;
            byte[] VRMdataRaw = File.ReadAllBytes(path);
            LoadVRMFromData(VRMdataRaw);
        }
        else
        {
            VRM0_Text.text = "VRM0\nFile not found";
            Debug.LogError("File not found: " + path);
        }
    }

    //Load VRM data on runtime
    //You can receive VRM over the network or file or other.
    public void LoadVRMFromData(byte[] VRMdataRaw)
    {
        try
        {
            UniGLTF.GlbLowLevelParser glbLowLevelParser = new UniGLTF.GlbLowLevelParser(null, VRMdataRaw);
            UniGLTF.GltfData gltfData = glbLowLevelParser.Parse();
            VRM.VRMData vrm = new VRM.VRMData(gltfData);
            VRM.VRMImporterContext vrmImporter = new VRM.VRMImporterContext(vrm);

            synchronizationContext.Post(async (_) =>
            {
                UniGLTF.RuntimeGltfInstance gltfInstance = await vrmImporter.LoadAsync(new VRMShaders.ImmediateCaller());
                gltfData.Dispose();
                vrmImporter.Dispose();

                Model = gltfInstance.Root;
                Model.transform.parent = this.transform;

                gltfInstance.EnableUpdateWhenOffscreen();
                gltfInstance.ShowMeshes();

                VRM0_Text.text = "VRM0\nLoad OK";
                ModelChenged = true;
            }, null);
        }
        catch (Exception e)
        {
            VRM0_Text.text = "VRM0\nLoad Error: " + e.Message;
        }
    }
}
