using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Physarum2D : MonoBehaviour
{
    public enum InitType
    {
        Random = 0,
        Texture = 0,
    }

    // size parameters 
    public float size=5f;
    public float senseDistance=0.1f;
    public int trailResolution = 1024;
    public float senseAngle = 30f;
    public float turnAngleSpeed = 30f;
    public float speed = 0.1f;
    private Matrix4x4 senseLeftMat;
    private Matrix4x4 senseRightMat;
    private Matrix4x4 turnLeftMat;
    private Matrix4x4 turnRightMat;
    public float depositRate=1.0f;
    public float diffuseRate=0.05f;
    public float decayRate = 0.99f;
    public int particleCount = 1000;
    public int threadNum = 8;

    public ComputeShader InitParticle;
    public ComputeShader PhysarumUpdate;
    public Material renderMaterial;

    public Material TrailVisualMat;


    private ComputeBuffer particleInfo,quad;

    public RenderTexture[] trailRT;
    public RenderTexture depositRT;
    private const int READ = 0;
    private const int WRITE = 1;

    struct ParticleInfo
    {
        public Vector3 position;
        public Vector3 radius;

    }

    private int InitParticleHandle;
    private int UpdateParticleHandle;
    private int DepositHandle;
    private int DiffuseTrailHandle;
    private int DecayTrailHandle;
    private int CleanHandle;

    private int SizeID            = Shader.PropertyToID("_Size");
    private int SenseDistanceID   = Shader.PropertyToID("_SenseDistance");
    private int SpeedID           = Shader.PropertyToID("_Speed");
    private int SenseLeftMatID    = Shader.PropertyToID("_SenseLeftMat");
    private int SenseRightMatID   = Shader.PropertyToID("_SenseRightMat");
    private int TurnLeftID        = Shader.PropertyToID("_TurnLeftMat");
    private int TurnRightID       = Shader.PropertyToID("_TurnRightMat");
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

    public void SwitchRT()
    {
        var tem = trailRT[0];
        trailRT[0] = trailRT[1];
        trailRT[1] = tem;
    }

    public void SetupParameters()
    {
        senseLeftMat = Matrix4x4.Rotate(Quaternion.AngleAxis(senseAngle,Vector3.forward));
        senseRightMat = Matrix4x4.Rotate(Quaternion.AngleAxis(-senseAngle, Vector3.forward));
    }

    public void UpdateParameters()
    {
        turnLeftMat = Matrix4x4.Rotate(Quaternion.AngleAxis(turnAngleSpeed * Time.deltaTime, Vector3.forward));
        turnRightMat = Matrix4x4.Rotate(Quaternion.AngleAxis(-turnAngleSpeed * Time.deltaTime, Vector3.forward));
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
            var rt = new RenderTexture(trailResolution, trailResolution, 0, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.Create();

            trailRT[i] = rt;
        }

        {
            var rt = new RenderTexture(trailResolution, trailResolution, 0, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.Create();

            depositRT = rt;
        }

        if (TrailVisualMat != null)
        {
            TrailVisualMat.SetTexture("_MainTex" , trailRT[0]);
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

        InitParticle.SetBuffer(InitParticleHandle, ParticleInfoID, particleInfo);
        InitParticle.SetFloat(SizeID,size);
        InitParticle.Dispatch(InitParticleHandle, particleCount / threadNum, 1, 1);

    }


    public void UpdateShader()
    {

        PhysarumUpdate.SetFloat(SizeID, size );
        PhysarumUpdate.SetFloat(SenseDistanceID, senseDistance);
        PhysarumUpdate.SetMatrix(SenseLeftMatID, senseLeftMat);
        PhysarumUpdate.SetMatrix(SenseRightMatID, senseRightMat);
        PhysarumUpdate.SetMatrix(TurnLeftID, turnLeftMat);
        PhysarumUpdate.SetMatrix(TurnRightID, turnRightMat);
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
        PhysarumUpdate.Dispatch(UpdateParticleHandle, particleGroupCount, 1 , 1 );


        PhysarumUpdate.SetTexture(DepositHandle, DepositID, depositRT);
        PhysarumUpdate.SetBuffer(DepositHandle, ParticleInfoID, particleInfo);
        PhysarumUpdate.Dispatch(DepositHandle, particleGroupCount, 1, 1);

        PhysarumUpdate.SetTexture(DiffuseTrailHandle, DepositID, depositRT);
        PhysarumUpdate.SetTexture(DiffuseTrailHandle, TrailReadID, trailRT[READ]);
        PhysarumUpdate.SetTexture(DiffuseTrailHandle, TrailWriteID, trailRT[WRITE]);
        PhysarumUpdate.Dispatch(DiffuseTrailHandle, texGroupCount, texGroupCount, 1);
        SwitchRT();

        PhysarumUpdate.SetTexture(DecayTrailHandle, TrailReadID, trailRT[READ]);
        PhysarumUpdate.SetTexture(DecayTrailHandle, TrailWriteID, trailRT[WRITE]);
        PhysarumUpdate.Dispatch(DecayTrailHandle, texGroupCount, texGroupCount, 1);
        SwitchRT();


    }

    public void CleanUpShader()
    {

        int texGroupCount = Mathf.CeilToInt((float)trailResolution / threadNum);

        PhysarumUpdate.SetTexture(CleanHandle, DepositID, depositRT);
        PhysarumUpdate.Dispatch(CleanHandle, texGroupCount, texGroupCount, 1);
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

}
