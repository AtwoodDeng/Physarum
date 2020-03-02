using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tex3DDisplay : MonoBehaviour
{

    public Shader shader;

    private  Material material;

    private  RenderTexture modelSource;

    public Physarum3D reference;
    public float fluidDensity = 1.0f;

    public void UpdateView(RenderTexture source, RenderTexture destination)
    {
        // default size of the viewing window 
        int width = source.width;
        int height = source.height;


        // set up the materials
        if (material == null)
            material = new Material(shader);

        if (modelSource == null)
        {
            modelSource = reference.trailRT[0];
        }

        var dir = Camera.main.transform.forward;
        var topDir = Camera.main.transform.up;
        var sideDir  = Camera.main.transform.right;
        var camPos = Camera.main.transform.position;

        var viewStartPos = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 1f));
        var ViewOffsetU = Camera.main.ViewportToWorldPoint(new Vector3(1f , 0, 1f)) - viewStartPos;
        var ViewOffsetV = Camera.main.ViewportToWorldPoint(new Vector3(0, 1f, 1f)) - viewStartPos;


        var viewDistance = Camera.main.transform.position.magnitude;
        var modelScale = Vector3.one * reference.size * 2f;
        var sampleRange = viewDistance + reference.size * Mathf.Sqrt(0.5f * 0.5f * 3);

        // set up the parameter
        material.SetVector("_ViewDir", dir.normalized);
        material.SetVector("_TopDir", topDir.normalized);
        material.SetVector("_SideDir", sideDir.normalized);

        material.SetVector("_CamPos" , camPos);
        material.SetVector("_ViewStartPos" , viewStartPos);
        material.SetVector("_ViewOffsetU", ViewOffsetU);
        material.SetVector("_ViewOffsetV", ViewOffsetV);
        material.SetFloat("_ViewDistance", viewDistance);
        material.SetVector("_View", new Vector4(width, height, 1f / width, 1f / height));
        material.SetFloat("_SampleRange" , sampleRange);

        material.SetTexture("_Model", modelSource);
        material.SetVector("_ModelScale", modelScale);
        material.SetFloat("_Density", fluidDensity);

        //material.SetVector("_LightDir", lightDir.normalized);

        material.SetInt("_UseLighting", 0);
        //material.SetFloat("_EnergySampleRange", energySampleRange);
        //material.SetFloat("_BeerLaw", beerLaw);
        material.SetInt("_DisplayChannel", 15);

        material.SetInt("_UseSlice", 0);

        Graphics.Blit(source, destination, material, 0);
    }


    public void OnRenderImage(RenderTexture source, RenderTexture destination )
    {
        UpdateView(source, destination);
    }


}
