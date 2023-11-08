using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

//TIPs: Please ensure there is no bulge on the surface
// 
public static class camExtension
{
    public static RenderTexture GetthisRT(this Camera cam,Vector2Int RenderResolution )
    {
        //Camera m_Camera = CameraObj.GetComponent<Camera>();
        RenderTexture rt = new RenderTexture(RenderResolution.x, RenderResolution.y, 16);
        cam.targetTexture = rt;
        cam.Render();
        return rt ;
    }
}

public class InteriorMapGenerator : MonoBehaviour
{
    private Material RenderToParaTexMaterial;
    private List<Camera> _cameragroup;
    private Vector3[] camerauvf;
    private Vector2[] boundinginfo;
    [Tooltip("The format like : Assets/interiormap ")]
    public string SavePath = "Assets/";
    private GameObject[] tempCamobj;
    public Vector2Int RenderResolution;
    
    public const int CAM_DEPTH = 0;
    // public const int CAM_LAYER = 5;
    
    [Header("Obj")]
    [Tooltip("WidthandHeight must be even number")]
    public Vector2Int WidthandHeight;
    [SerializeField] public GameObject[] ObjDay;
    [SerializeField] public GameObject[] ObjNight;
   
    
    private static readonly int PreprojectedTex = Shader.PropertyToID("_PreprojectedTex");
    private static readonly int BAinfo = Shader.PropertyToID("_BAinfo");
    private static readonly int UVInfo = Shader.PropertyToID("_UVInfo");
    
    public void CreateCamera()
    {
        if(ObjDay.Length != ObjNight.Length)
        {  
            Debug.Log("Day and Night Count Not Match"); 
            return;
        }
        if((WidthandHeight.x * WidthandHeight.y) != ObjDay.Length * 2)
        {  
            Debug.Log("Width and Height Not Match"); 
            return;
        }
        
        //initialize
        int lenth = WidthandHeight.x * WidthandHeight.y;
        _cameragroup = new List<Camera>(lenth);
        camerauvf = new Vector3[lenth];
        tempCamobj = new GameObject[lenth];
        boundinginfo = new Vector2[lenth];
        
        //creat cam and basic
        int index = 0;
        for (int i = 0; i < tempCamobj.Length; i++)
        {
            tempCamobj[i] = new GameObject("cam" + index);
            Camera tempcam = tempCamobj[i].AddComponent<Camera>();
           _cameragroup.Add(tempcam);
           index++;
        }
        
        Vector2 uvunit = new Vector2(1.0f / WidthandHeight.x,1.0f /WidthandHeight.y);
        int cut = WidthandHeight.x / 2;
        float halftan = Mathf.Tan(53.1f / 2 * (float) Math.PI / 180f);//can make const?
        
        //Dayobj
        for (int i = 0 ; i < ObjDay.Length;i++)
        {
            GameObject RenderObj = ObjDay[i];
            Bounds Objbound = getBound(RenderObj);
            Vector3 pos = Objbound.center;
            float boundingx = Objbound.extents.x;//cam move dir
            float longbound = Math.Max(Objbound.extents.y, Objbound.extents.z);
            float distance = boundingx + longbound / halftan;

            float BAdepth = longbound / ((distance + boundingx) * halftan); 
            
            //cameraseting
            _cameragroup[i].fieldOfView = 53.1f;
            _cameragroup[i].transform.localPosition = pos + new Vector3( distance,0,0 );
            _cameragroup[i].transform.LookAt(pos);
             _cameragroup[i].depth = CAM_DEPTH;
             _cameragroup[i].cullingMask = 1 << 0;
            // // cam.gameObject.layer = CAM_LAYER;
             _cameragroup[i].clearFlags = CameraClearFlags.Skybox;
             // cam.orthographic = true; 
            // cam.orthographicSize = 1; 
            // cam.nearClipPlane = -2.7f; 
            // cam.farClipPlane = 2.92f; 
            _cameragroup[i].rect = new Rect(0, 0, 1f, 1f);
            
            camerauvf[i] = new Vector3( uvunit.x * (i % cut), uvunit.y * (i / cut), BAdepth);
            boundinginfo[i] = new Vector2(Objbound.extents.z, Objbound.extents.y);
        }
        
        //Nightobj
        for (int i = 0 ; i < ObjNight.Length;i++)
        {
            int ri = i + ObjDay.Length;
            GameObject RenderObj = ObjNight[i];
            Bounds Objbound = getBound(RenderObj);
            Vector3 pos = Objbound.center;
            float boundingx = Objbound.extents.x;//cam move dir
            float longbound = Math.Max(Objbound.extents.y, Objbound.extents.z);
            float distance = boundingx + longbound / halftan;

            float BAdepth = longbound / ((distance + boundingx) * halftan);
            //cameraseting
            _cameragroup[ri].transform.localPosition = pos + new Vector3( distance,0,0 );
            _cameragroup[ri].transform.LookAt(pos);
            _cameragroup[ri].depth = CAM_DEPTH;
            _cameragroup[ri].cullingMask = 1 << 0;
            // cam.gameObject.layer = CAM_LAYER;
            _cameragroup[ri].clearFlags = CameraClearFlags.Skybox;
            _cameragroup[ri].fieldOfView = 53.1f;
            // cam.orthographic = true; 
            // cam.orthographicSize = 1; 
            // cam.nearClipPlane = -2.7f;
            // cam.farClipPlane = 2.92f; 
            _cameragroup[ri].rect = new Rect(0, 0, 1f, 1f);
            
            camerauvf[ri] = new Vector3( uvunit.x * (i % cut) + 0.5f, uvunit.y * (i / cut),BAdepth);
            boundinginfo[ri] = new Vector2(Objbound.extents.z, Objbound.extents.y);
        }
        
    }

    public void render1()
    {
        if (_cameragroup.Count == 0)
        {
            Debug.Log("Please Create camera first");
        }
        
        //Final RT
        RenderTexture FinalTex = RenderTexture.GetTemporary((int)(RenderResolution.x * WidthandHeight.x), (int)(RenderResolution.y * WidthandHeight.y), 16, RenderTextureFormat.ARGB32);
        //Temp RT
        RenderTexture nRT = RenderTexture.GetTemporary((int)(RenderResolution.x * WidthandHeight.x), (int)(RenderResolution.y * WidthandHeight.y));
        RenderToParaTexMaterial = new Material(Shader.Find("InteriorMapGeneratorShader/atlasshader"));
        RenderToParaTexMaterial.SetTexture(PreprojectedTex, FinalTex);
        
        for (int backCameraID = 0; backCameraID < _cameragroup.Count; backCameraID++)
        {
            //Set render uv
            RenderToParaTexMaterial.SetVector(UVInfo,
                new Vector4(camerauvf[backCameraID].x, camerauvf[backCameraID].y, 1.0f / WidthandHeight.x, 1.0f / WidthandHeight.y));
            RenderToParaTexMaterial.SetVector(BAinfo, new Vector3(boundinginfo[backCameraID].x, boundinginfo[backCameraID].y, camerauvf[backCameraID].z));
            //Altas
            Graphics.Blit(_cameragroup[backCameraID].GetthisRT(RenderResolution), nRT, RenderToParaTexMaterial);
            //Copy to FinalTex
            Graphics.Blit(nRT, FinalTex);
        }

        _SaveRenderTexture(FinalTex);
        RenderTexture.ReleaseTemporary(FinalTex);
        RenderTexture.ReleaseTemporary(nRT);
    }
    
    private void _SaveRenderTexture(RenderTexture rt)
    {
        RenderTexture.active = rt;
        Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        png.Apply();
        byte[] bytes = png.EncodeToPNG();
        string path = string.Format(SavePath + "/rt_{0}_{1}_{2}.png", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        
        if(!System.IO.Directory.Exists(SavePath)){
            System.IO.Directory.CreateDirectory(SavePath);
            print ("Make new folder");
        }
        
        // FileStream fs = File.Open(path, FileMode.Create);
        // BinaryWriter writer = new BinaryWriter(fs);
        // writer.Write(bytes);
        // writer.Flush();
        // writer.Close();
        // fs.Close();
        System.IO.File.WriteAllBytes(path, png.EncodeToPNG());
        DestroyImmediate(png);
        //png = null;
        Debug.Log("Save success"+path);
        
        _cameragroup.Clear();
        foreach (var camobj in tempCamobj)
        {
            DestroyImmediate(camobj);
        }
    }

    public void ClearCamera()
    {
        _cameragroup.Clear();
        foreach (var camobj in tempCamobj)
        {
            DestroyImmediate(camobj);
        }
    }
    
    //center not the pos, recalculating
    protected static Bounds getBound(GameObject model)
    {
        Vector3 fakecenter = Vector3.zero;
        CalculatefakeCenter(model, ref fakecenter);
        Bounds resultBounds = new Bounds(fakecenter, Vector3.zero);
        CalculateBounds(model, ref resultBounds);
        return resultBounds;
    }

    protected static void CalculatefakeCenter(GameObject model, ref Vector3 result)
    {
        Renderer renders = model.GetComponentInChildren<Renderer>();
        if(!renders)
        {   
            Debug.Log("No renderer in obj,check and recreate the cam");
            return;
        }
        result = renders.bounds.center;
    }
    protected static void CalculateBounds(GameObject model, ref Bounds resultBounds)
    {
        Renderer[] renders = model.GetComponentsInChildren<Renderer>();
        if(renders == null) return;
        foreach (Renderer child in renders)
        {
            resultBounds.Encapsulate(child.bounds);
        }
    }
    
}
