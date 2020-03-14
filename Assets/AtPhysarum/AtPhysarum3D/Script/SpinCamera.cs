using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinCamera : MonoBehaviour
{
    public float rotateAngleSpeed=30f;
    [Range(0,10f)]
    public float radius = 5f;
    [Range(-5f,5f)]
    public float YOffset = 2f;
    public  Transform target;
    public void Update()
    {
        transform.position = new Vector3( 
            Mathf.Cos(rotateAngleSpeed * Time.time * Mathf.Deg2Rad) * radius,YOffset,
            Mathf.Sin(rotateAngleSpeed * Time.time * Mathf.Deg2Rad) * radius);

        transform.LookAt(target);

    }
}
