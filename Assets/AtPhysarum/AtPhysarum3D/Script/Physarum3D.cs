using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Physarum3D : MonoBehaviour
{
    public enum InitType
    {
        Cube,
        Sphere,
    }

    [Header("Init")]
    public InitType initType;
    public int particleCount = 1000;
    public int trailResolution = 128;

    [Header("Update")]
    // size parameters 
    public float size=5f;
    public float senseDistance=0.1f;
    public float senseAngle = 30f;
    public float turnAngleSpeed = 30f; 
    public float speed = 0.1f;
    private Matrix4x4[] senseMats;
    private Matrix4x4[] turnMats;
    [Range(2,8)]
    public int SensorCount = 4;
    [Range(0,10f)]
    public float depositRate=4.0f;
    [Range(0,1f/27f)]
    public float diffuseRate=0.05f;
    [Range(-1f,1f)]
    public float decayRate = 0.7f;
    const int threadNum = 8;

    [Header("Shader & Material")]
    public ComputeShader InitParticle;
    public ComputeShader PhysarumUpdate;
    public Material renderMaterial;

    private ComputeBuffer particleInfo,quad;
    public RenderTexture[] trailRT;
    private RenderTexture depositRT;
    private const int READ = 0;
    private const int WRITE = 1;

    struct ParticleInfo
    {
        public Vector3 position;
        public Vector3 velocity;

    }

    private int InitParticleHandle;
    private int UpdateParticleHandle;
    private int DepositHandle;
    private int DiffuseTrailHandle;
    private int DecayTrailHandle;
    private int CleanHandle;

    private int InitTypeID        = Shader.PropertyToID("_InitType");
    private int SizeID            = Shader.PropertyToID("_Size");
    private int SenseDistanceID   = Shader.PropertyToID("_SenseDistance");
    private int SpeedID           = Shader.PropertyToID("_Speed");

    private int SenseMatID = Shader.PropertyToID("_SenseMat");
    private int TurnMatID = Shader.PropertyToID("_TurnMat");
    private int SensorCountID = Shader.PropertyToID("_SensorCount");
    //private int SenseLeftMatID    = Shader.PropertyToID("_SenseLeftMat");
    //private int SenseRightMatID   = Shader.PropertyToID("_SenseRightMat");
    //private int TurnLeftID        = Shader.PropertyToID("_TurnLeftMat");
    //private int TurnRightID       = Shader.PropertyToID("_TurnRightMat");
    private int DepositRateID     = Shader.PropertyToID("_DepositRate");
    private int DiffuseRateID     = Shader.PropertyToID("_DiffuseRate");
    private int DecayRateID       = Shader.PropertyToID("_DecayRate");
    private int DeltaTimeID       = Shader.PropertyToID("_DeltaTime");
    private int TrailResolutionID = Shader.PropertyToID("_TrailResolution");
    private int ParticleCountID   = Shader.PropertyToID("_ParticleCount");
    private int TrailReadID       = Shader.PropertyToID("TrailRead");
    private int TrailWriteID      = Shader.PropertyToID("TrailWrite");
    private int DepositID         = Shader.PropertyToID("DepositTex");
    private int ParticleInfoID    = Shader.PropertyToID("ParticleInfoBuffer");

    private const int maxSensorNum = 9;

    public void SwitchRT()
    {
        var tem = trailRT[0];
        trailRT[0] = trailRT[1];
        trailRT[1] = tem;
    }

    public void SetupParameters()
    {
        senseMats = new Matrix4x4[maxSensorNum];
        turnMats = new Matrix4x4[maxSensorNum];
        float sensorAngleInterval = 360f / SensorCount;


        for (int i = 0; i < maxSensorNum; ++i)
        {

            senseMats[i] = Matrix4x4.Rotate(Quaternion.AngleAxis(senseAngle 
                , Quaternion.AngleAxis(i * sensorAngleInterval , Vector3.up ) * Vector3.forward ));
        }



    }

    public void UpdateParameters()
    {
        float sensorAngleInterval = 360f / SensorCount;
        for (int i = 0; i < maxSensorNum; ++i)
        {
            turnMats[i] = Matrix4x4.Rotate(Quaternion.AngleAxis(turnAngleSpeed * Time.deltaTime
                , Quaternion.AngleAxis(i * sensorAngleInterval, Vector3.up) * Vector3.forward));

        }

    }

    public void SetupShader()
    {
        InitParticleHandle = InitParticle.FindKernel("InitParticle");
        UpdateParticleHandle = PhysarumUpdate.FindKernel("UpdateParticle");
        DepositHandle = PhysarumUpdate.FindKernel("Deposit");
        DiffuseTrailHandle = PhysarumUpdate.FindKernel("DiffuseTrail");
        DecayTrailHandle = PhysarumUpdate.FindKernel("DecayTrail");
        CleanHandle = PhysarumUpdate.FindKernel("Clean");
    }

    public void SetupRT()
    {
        trailRT = new RenderTexture[2];
        for (int i = 0; i < 2; ++i)
        {
            var rt = new RenderTexture(trailResolution, trailResolution, 0, RenderTextureFormat.RFloat);
            rt.enableRandomWrite = true;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.dimension = TextureDimension.Tex3D;
            rt.volumeDepth = trailResolution;

            rt.Create();

            trailRT[i] = rt;
        }

        {
            var rt = new RenderTexture(trailResolution, trailResolution, 0, RenderTextureFormat.RFloat);
            rt.enableRandomWrite = true;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.dimension = TextureDimension.Tex3D;
            rt.volumeDepth = trailResolution;
            rt.Create();

            depositRT = rt;
        }

    }

    public void SetupBuffer()
    {
        ReleaseBuffers();

        particleInfo = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(ParticleInfo)));

        quad = new ComputeBuffer(6,Marshal.SizeOf(typeof(Vector3)));
        quad.SetData(new[]
        {
            new Vector3(-0.5f,0.5f),
            new Vector3(0.5f,0.5f),
            new Vector3(0.5f,-0.5f),
            new Vector3(0.5f,-0.5f),
            new Vector3(-0.5f,-0.5f),
            new Vector3(-0.5f,0.5f)
        });

        InitParticle.SetInt(InitTypeID,(int)initType);
        InitParticle.SetBuffer(InitParticleHandle, ParticleInfoID, particleInfo);
        InitParticle.SetFloat(SizeID,size);
        InitParticle.Dispatch(InitParticleHandle, particleCount / threadNum, 1, 1);

    }


    public void UpdateShader()
    {

        PhysarumUpdate.SetFloat(SizeID, size );
        PhysarumUpdate.SetFloat(SenseDistanceID, senseDistance);
        PhysarumUpdate.SetMatrixArray(SenseMatID,senseMats);
        PhysarumUpdate.SetMatrixArray(TurnMatID,turnMats);
        PhysarumUpdate.SetInt(SensorCountID,SensorCount);
        PhysarumUpdate.SetFloat(SpeedID,speed);
        PhysarumUpdate.SetFloat(DepositRateID, depositRate);
        PhysarumUpdate.SetFloat(DiffuseRateID, diffuseRate);
        PhysarumUpdate.SetFloat(DecayRateID, decayRate);
        PhysarumUpdate.SetFloat(DeltaTimeID,Time.deltaTime);
        PhysarumUpdate.SetInt(TrailResolutionID, trailResolution);
        PhysarumUpdate.SetInt(ParticleCountID, particleCount);

        int particleGroupCount = Mathf.CeilToInt((float) particleCount / threadNum);
        int texGroupCount = Mathf.CeilToInt((float)trailResolution / threadNum);

        PhysarumUpdate.SetTexture(UpdateParticleHandle, TrailReadID, trailRT[READ]);
        PhysarumUpdate.SetBuffer(UpdateParticleHandle, ParticleInfoID, particleInfo);
        PhysarumUpdate.Dispatch(UpdateParticleHandle, particleGroupCount, 1, 1);

        PhysarumUpdate.SetTexture(DepositHandle, DepositID, depositRT);
        PhysarumUpdate.SetBuffer(DepositHandle, ParticleInfoID, particleInfo);
        PhysarumUpdate.Dispatch(DepositHandle, particleGroupCount, 1, 1);

        PhysarumUpdate.SetTexture(DiffuseTrailHandle, DepositID, depositRT);
        PhysarumUpdate.SetTexture(DiffuseTrailHandle, TrailReadID, trailRT[READ]);
        PhysarumUpdate.SetTexture(DiffuseTrailHandle, TrailWriteID, trailRT[WRITE]);
        PhysarumUpdate.Dispatch(DiffuseTrailHandle, texGroupCount, texGroupCount, texGroupCount);
        SwitchRT();

        PhysarumUpdate.SetTexture(DecayTrailHandle, TrailReadID, trailRT[READ]);
        PhysarumUpdate.SetTexture(DecayTrailHandle, TrailWriteID, trailRT[WRITE]);
        PhysarumUpdate.Dispatch(DecayTrailHandle, texGroupCount, texGroupCount, texGroupCount);
        SwitchRT();


    }

    public void CleanUpShader()
    {
        int texGroupCount = Mathf.CeilToInt((float)trailResolution / threadNum);

        PhysarumUpdate.SetTexture(CleanHandle, DepositID, depositRT);  
        PhysarumUpdate.Dispatch(CleanHandle, texGroupCount, texGroupCount, texGroupCount);
    }


    public void ReleaseBuffers()
    {
        if ( particleInfo != null )  particleInfo.Release();
        if ( quad != null )  quad.Release();

        if (trailRT != null)
        {
            foreach (var rt in trailRT)
            {
                rt.Release();
            }

            trailRT = null;
        }

        if ( depositRT != null ) depositRT.Release();
    }

    public void Awake()
    {
        SetupParameters();
        SetupShader();
        SetupBuffer();
        SetupRT();
    }

    void Update()
    {
        UpdateParameters();
        UpdateShader();
    }

    void LateUpdate()
    {
        CleanUpShader();
    }

    void OnRenderObject()
    {
        renderMaterial.SetBuffer("particles", particleInfo);
        renderMaterial.SetBuffer("quad", quad);

        renderMaterial.SetPass(0);

        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, particleCount);

    }

    public void OnDrawGizmos()
    {

        SetupParameters();

        Gizmos.color = Color.red;

        Vector3 dir = new Vector3(speed,senseDistance,diffuseRate);
        Gizmos.DrawRay(Vector3.zero,dir);

        Gizmos.color = Color.yellow;
        
        for (int i = 0; i < SensorCount; ++i)
        {
            Gizmos.DrawRay(Vector3.zero, senseMats[i] * dir );    
        }
    }

}
