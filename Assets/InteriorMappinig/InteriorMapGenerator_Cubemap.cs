using System;
using System.Collections.Generic;
using UnityEngine;

public class InteriorMapGenerator_Cubemap : MonoBehaviour
{

    private Material RenderToParaTexMaterial;
    private List<Camera> _cameragroup;
    private Vector3[] camerauvf;
    private Vector2[] boundinginfo;

    private GameObject[] tempCamobj;
    public Vector2Int RenderResolution;
    public const int CAM_DEPTH = 0;
    
    [Tooltip("The format like : Assets/interiormap ")]
    [SerializeField]public string savePath = "Assets/";
    //public const int CAM_LAYER = 5;
    
    [Header("Obj")]
    [Tooltip("WidthandHeight must be even number and equal to real tex")]
    public Vector2Int WidthAndHeight;
    public Texture[] _CubemapDay;
    public Texture[] _CubemapNight;
  
    private List<GameObject> ObjDay = new List<GameObject>();
    private List<GameObject> ObjNight = new List<GameObject>();
    private Material[] daytemp;
    private Material[] nighttemp;
    
    private static readonly int PreprojectedTex = Shader.PropertyToID("_PreprojectedTex");
    private static readonly int BAinfo = Shader.PropertyToID("_BAinfo");
    private static readonly int UVInfo = Shader.PropertyToID("_UVInfo");

    private void OnEnable()
    {
        savePath = "Assets/";
    }

    public void Mergeall()
    {
        CreateCamera();
        RendertoTex();
    }

    public void CreatBox()
    {
        daytemp = new Material[_CubemapDay.Length];
        nighttemp = new Material[_CubemapNight.Length];
        ObjDay.Clear();
        ObjNight.Clear();
        int collum = WidthAndHeight.x / 2;

        for (int i = 0; i < _CubemapDay.Length; i++)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Quad);
            daytemp[i] = new Material(Shader.Find( "InteriorMapGeneratorShader/Cubemapshader"));
            daytemp[i].SetTexture("_RoomCube",_CubemapDay[i]);
            box.transform.position = new Vector3(0,i / collum,i % collum) ;
            box.transform.Rotate(0,-90,0);
            box.GetComponent<MeshRenderer>().material = daytemp[i];
            ObjDay.Add(box);
        }
        
        for (int i = 0; i < _CubemapNight.Length; i++)
        {
            nighttemp[i] = new Material(Shader.Find( "InteriorMapGeneratorShader/Cubemapshader"));
            nighttemp[i].SetTexture("_RoomCube",_CubemapNight[i]);
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Quad);
            box.transform.position = new Vector3(0,i / collum,(i % collum) + collum) ;
            box.transform.Rotate(0,-90,0);
            box.GetComponent<MeshRenderer>().material = nighttemp[i];
            ObjNight.Add(box);
        }
    }
    
    private void CreateCamera()
    {
        if(ObjDay.Count != ObjNight.Count)
        {  
            Debug.Log("Day and Night Count Not Match"); 
            return;
        }
        if((WidthAndHeight.x * WidthAndHeight.y) != ObjDay.Count * 2)
        {  
            Debug.Log("Width and Height Not Match the Number"); 
            return;
        }
        
        //initialize
        int lenth = WidthAndHeight.x * WidthAndHeight.y;
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
        
        Vector2 uvunit = new Vector2(1.0f / WidthAndHeight.x,1.0f /WidthAndHeight.y);
        int cut = WidthAndHeight.x / 2;
        float halftan = Mathf.Tan(53.1f / 2 * (float) Math.PI / 180f);//can make const?
        
        //Dayobj
        for (int i = 0 ; i < ObjDay.Count;i++)
        {
            GameObject RenderObj = ObjDay[i];
            Bounds Objbound = getBound(RenderObj);
            Vector3 pos = Objbound.center;
            float boundingx = Objbound.extents.x;//cam move dir
            float longbound = Math.Max(Objbound.extents.y, Objbound.extents.z);
            float distance = boundingx + longbound / halftan;

            float BAdepth = daytemp[i].GetFloat("_RoomDepth");
            
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
        for (int i = 0 ; i < ObjNight.Count;i++)
        {
            int ri = i + ObjDay.Count;
            GameObject RenderObj = ObjNight[i];
            Bounds Objbound = getBound(RenderObj);
            Vector3 pos = Objbound.center;
            float boundingx = Objbound.extents.x;//cam move dir
            float longbound = Math.Max(Objbound.extents.y, Objbound.extents.z);
            float distance = boundingx + longbound / halftan;

            float BAdepth = nighttemp[i].GetFloat("_RoomDepth");
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

    private void RendertoTex()
    {
        if (_cameragroup.Count == 0)
        {
            Debug.Log("Please Load Tex first");
        }
        
        //Final RT
        RenderTexture FinalTex = RenderTexture.GetTemporary((int)(RenderResolution.x * WidthAndHeight.x), (int)(RenderResolution.y * WidthAndHeight.y), 16, RenderTextureFormat.ARGB32);
        //Temporary RT
        RenderTexture nRT = RenderTexture.GetTemporary((int)(RenderResolution.x * WidthAndHeight.x), (int)(RenderResolution.y * WidthAndHeight.y));
        RenderToParaTexMaterial = new Material(Shader.Find("InteriorMapGeneratorShader/atlasshader"));
        RenderToParaTexMaterial.SetTexture(PreprojectedTex, FinalTex);
        
        for (int backCameraID = 0; backCameraID < _cameragroup.Count; backCameraID++)
        {
            //Mat info, uv or so 
            RenderToParaTexMaterial.SetVector(UVInfo,
                new Vector4(camerauvf[backCameraID].x, camerauvf[backCameraID].y, 1.0f / WidthAndHeight.x, 1.0f / WidthAndHeight.y));
            RenderToParaTexMaterial.SetVector(BAinfo, new Vector3(boundinginfo[backCameraID].x, boundinginfo[backCameraID].y, camerauvf[backCameraID].z));
            //Mat
            Graphics.Blit(_cameragroup[backCameraID].GetthisRT(RenderResolution), nRT, RenderToParaTexMaterial);
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
        string path = string.Format(savePath + "rt_{0}_{1}_{2}.png", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        //"Assets/interiormappinig/image"
        
        if(!System.IO.Directory.Exists(savePath)){
            System.IO.Directory.CreateDirectory(savePath);
            print ("Creat Path");
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
        Debug.Log("Save Success"+path);
        
        _cameragroup.Clear();
        foreach (var camobj in tempCamobj)
        {
            DestroyImmediate(camobj);
        }
        foreach (var quadobj in ObjDay)
        {
            DestroyImmediate(quadobj);
        }foreach (var quadobj in ObjNight)
        {
            DestroyImmediate(quadobj);
        }
        
        
    }

    //center Not pos, so give another chance 
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
