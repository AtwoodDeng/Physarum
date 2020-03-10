#ifndef PARTICLE_COMMON
#define PARTICLE_COMMON

#define PI 3.1415926

float _Test;

struct ParticleInfo
{
    float3 pos;
    float3 vel;
};


float rand(float id )
{
    return frac( sin( id * 18.319 ));
}

float rand(float3 co)
{
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 1.2287 );
}

float3 rand3(float id )
{
    return frac(sin( float3(12.9898, 78.233, 45.5432) * id * 1.5453  ) + sin( float3(31.9898, 16.233, 5.5432) * id * 2.5453 ));
}


float2 rand2(float id )
{
    return frac(sin( float2(12.9898, 7.233) * id * 2.123 ) + sin( float2(83.9898, 125.233) * id * 1.123 ));
}

float3 rand_sphere(float u, float v, float radius)
{
    float theta = 2 * PI * u;
    float phi = acos(2 * v - 1);
    float x = radius * sin(phi) * cos(theta);
    float y = radius * sin(phi) * sin(theta);
    float z = radius * cos(phi);
    return float3(x, y, z);
}

float3 RepeatPosition( float3 pos , float size )
{
    return ( frac( pos /( size * 2.0) + float3(1,1,1) * 0.5 ) - float3(1,1,1) * 0.5 ) * size * 2.0;
}

uint3 RepeatTrailPos( uint3 id , int resolution )
{
    return ( id + resolution ) % resolution;
}


uint3 GetTextureID( float3 pos , float size, int resolution )
{
    return round( ( pos / ( size * 2.0 ) + 0.5 ) * resolution );
}


#endif

