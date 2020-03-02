#include "UnityCG.cginc"
#define MIN_SAMPLE_COUNT 512	
#define MAX_SAMPLE_COUNT 1024

// #define THICKNESS 6500.0
#define THICKNESS _CloudThickness
#define CENTER 4750.0
#define CLOUD_HEIGHT _CloudHeight

#define earthRadius 6500000.0

float _Density;
sampler3D _Model;
float3 _ModelScale;
float4 _LightDir;
float _EnergySampleRange;

float _BeerLaw;
float _SilverIntensity;
float _SilverSpread;

int _SampleCountRate ;
int _DisplayChannel;
float _SampleRange;


int _ViewDetail;
int _ViewCurl;


float _UseSlice;
int _UseLighting;

float _SliceOffset;
int _Plane;

float SampleDensityAdvance(float3 worldPos , int channel , int slice  ) {
	
	// sample the density from the 3d texture
	half4 densitySampleUV = half4((worldPos.xyz / _ModelScale), 0);

	if ( abs( densitySampleUV.x ) > 0.5 || abs( densitySampleUV.y ) > 0.5 || abs( densitySampleUV.z ) > 0.5 )
		return 0;

	if ( _UseSlice && slice )
	{
		float value = ( _Plane == 0 ) * worldPos.z + ( _Plane == 1 ) * worldPos.y + ( _Plane == 2 ) * worldPos.x + 0.5;
		
		if ( value > _SliceOffset - 0.01 && value < _SliceOffset + 0.01 )
			return 1;
	}

	densitySampleUV.xyz = (densitySampleUV.xyz + half3(0.5, 0.5 , 0.5));
	float4 data = tex3Dlod(_Model , half4( densitySampleUV.xyz , 0 ) );

	//float4 data = length( worldPos ) < 1.0 ? 1: 0; 
	
	return data.r * (channel == 0 ) + data.g * (channel == 1) + data.b * (channel == 2) + data.a * (channel == 3);
}


float Remap(float original_value, float original_min, float original_max, float new_min, float new_max)
{
	return new_min + (((original_value - original_min) / (original_max - original_min)) * (new_max - new_min));
}

float RemapClamped(float original_value, float original_min, float original_max, float new_min, float new_max)
{
	return new_min + (saturate((original_value - original_min) / (original_max - original_min)) * (new_max - new_min));
}


float HenryGreenstein(float g, float cosTheta) {
	float pif = 1.0;// (1.0 / (4.0 * 3.1415926f));
	float numerator = 1 - g * g ;
	float denominator = pow(1 + g * g - 2 * g * cosTheta, 1.5);
	return pif * numerator / denominator;
}

float BeerLaw(float d, float cosTheta) {
	d *= _BeerLaw;
	float firstIntes = exp(-d);

	return firstIntes;
	// A minor optimization.
	// the effect is not obvious
	// maybe avoide some extreme situation?
	float secondIntens = exp(-d * 0.25) * 0.7;
	float secondIntensCurve = 0.5;
	float tmp = max(firstIntes, secondIntens * RemapClamped(cosTheta, 0.7, 1.0, secondIntensCurve, secondIntensCurve * 0.25));
	return tmp;
}

float Inscatter(float3 worldPos,float dl, float cosTheta , int channel ) {

	// return BeerLaw( -2 * dl  , cosTheta  );
	float heightPercent = saturate( worldPos.y / _ModelScale.y );
	float lodded_density = saturate(SampleDensityAdvance(worldPos , channel , 0 ));
	float depth_probability = 0.05 + pow(lodded_density, RemapClamped(heightPercent, 0.3, 0.85, 0.5, 2.0));
	depth_probability = lerp(depth_probability, 1.0, saturate(dl * 50));
	float vertical_probability = pow(max(0, Remap(heightPercent, 0.0, 0.14, 0.1, 1.0)), 0.8);

	return saturate(depth_probability * vertical_probability);
}

// d for the average density of cloud in the sun direction
float Energy(float3 worldPos, float d, float cosTheta , int channel ) {
	return BeerLaw( d , cosTheta ) * Inscatter(worldPos , d , cosTheta , channel );
}

float GetButtomCloudMask( float3 dir )
{
	if (dir.y > 0.5) // greater than 30 degree
		return 1;

	if (dir.y < 0)
		return 0;

	return saturate( dir.y / 0.5);
}


half rand(half3 co)
{
	return frac(sin(dot(co.xyz, half3(12.9898, 78.233, 45.5432))) * 43758.5453) - 0.5;
}

float SampleEnergy(float3 worldPos, float3 viewDir , int channel ) {
#define DETAIL_ENERGY_SAMPLE_COUNT 8

	float totalSample = 0;
	int mipmapOffset = 0.5;
	float sampleRange = _EnergySampleRange ;
	
	const half3 RandomUnitSphere[8] = { 
	{ -0.3876953 ,  0.1875, 0.28125 },
	{ -0.3515625, 0.02636719, -0.2070313 },
	{ -0.3632813, 0.4492188, -0.4609375 },
	{ -0.1914063, -0.2519531, 0.4560547 },
	{ -0.4570313, 0.3515625, 0.04846191 },
	{ 0.3554688, 0.4296875, -0.2451172 },
	{  0.1875, -0.3876953 ,  0.28125 },
	{ 0.02636719, -0.3515625,  -0.2070313 },
	};
   
	// sampling from this position to the sun's position 
	[loop]
	for (float i = 0; i < DETAIL_ENERGY_SAMPLE_COUNT; i++) {
		// half3 rand3 = half3(rand(half3(0, i, 0)), rand(half3(1, i, 0)), rand(half3(0, i, 1)));
		half3 rand3 = RandomUnitSphere[i] ;
		half3 direction = (_LightDir) * 2 + normalize(rand3);
		direction = normalize(direction);
		float3 samplePoint = worldPos 
			+ (direction * i / DETAIL_ENERGY_SAMPLE_COUNT) * sampleRange;
		totalSample += SampleDensityAdvance(samplePoint , channel , 0 );
		mipmapOffset += 0.5;
	}  
  
	totalSample *=  _Density / DETAIL_ENERGY_SAMPLE_COUNT;

	// calculate the energy
	float energy = Energy(worldPos , totalSample.r , dot(viewDir, (_LightDir) ) , channel );
	return energy;
}

// basically , return the intensity as rgb and density as alpha
float4 MergeColorAdvance( float4 currSample )
{
	float depth = currSample.g;
	float intensity = currSample.r;
	float density = currSample.a;

	half4 result;

	result.a = saturate( density );
	result.rgb = intensity ;

	return result;
}

// sample the density in the pixel by raymarching

float4 GetDentisyAdvance(float3 startPos, float3 dir , float raymarchOffset, out float4 intensity4 ) 
{
	// calculate the sampling step
	int sample_count = MIN_SAMPLE_COUNT;
	float sample_step = _SampleRange / sample_count ;

	// then start to calculate the alpha( density ) and intensity
	float alpha = 0;
	float intensity = 0;

	// initialize the variables 
	float4 density = float4(0,0,0,0);
	intensity4 = float4(0,0,0,0);
	bool detailedSample = false;
	int missedStepCount = 0;

	// raymarching
	raymarchOffset = 0.5;
	float raymarchDistance = raymarchOffset * sample_step;
	[loop]
	for( int c = 0 ; c < 4 ; c++ ) { // for each channel

		if ( ( _DisplayChannel >> c ) & 1 ) {
			alpha = 0;
			intensity = 0;
			raymarchDistance = raymarchOffset * sample_step;
			for (int j = 0; j < sample_count ; j++) {  // for each sample

				// when the alpha reach 1 , then stop the processing
				if ( alpha < 1 )
				{
				float3 rayPos = startPos + dir * raymarchDistance;
				// when not detecting the cloud( density == 0 )
				// do the sampling in a larger step
				if (!detailedSample) {
					float sampleResult = SampleDensityAdvance(rayPos , c , 1 );
					if (sampleResult.r > 0) { // when the cloud is detected(density > 0) , go back and do the detailed sampling
						detailedSample = true;
						raymarchDistance -= sample_step * 3;
						missedStepCount = 0;
						continue;
					}
					else {
						raymarchDistance += sample_step * 3;
					}
				}
				else // the detailed sampling, get the intensity(lighting information)
				{
					float sampleResult = SampleDensityAdvance(rayPos , c , 1  );

					if (sampleResult.r <= 0) {
						missedStepCount++;
						if (missedStepCount > 10) {
							detailedSample = false;
						}
					}
					else
					{
					
						// make the sampledAlpha fix with sample count
						float sampledAlpha = sampleResult.r * sample_step * _Density  ;
						float sampledEnergy = SampleEnergy(rayPos, dir , c );
						intensity += (1 - alpha) * sampledEnergy * sampledAlpha ;
						alpha += (1 - alpha) * sampledAlpha ;
						if (alpha > 1) {
							// intensity /= alpha;
							alpha = 1;
						}
					}

					float sampleResultWithSlice = SampleDensityAdvance(rayPos , c , 1  );


					raymarchDistance += sample_step;
				}
				}
			}


			// assign the color and intensity by channel type
			if ( c == 0 )
			{
				density.r = alpha;
				intensity4.r = intensity;

			}else if ( c == 1 )
			{
				density.g = (_DisplayChannel == 4 ) * pow( alpha , 0.33) +  (_DisplayChannel != 4 ) * alpha;
				intensity4.g = intensity;

			}else if ( c == 2 )
			{
				density.b = (_DisplayChannel == 8 ) * pow( alpha , 0.2) + (_DisplayChannel != 8 ) * alpha;
				intensity4.b = intensity;
			}else if ( c == 3 )
			{
				density.a = alpha;
				intensity4.a = intensity;
			}
		}
	}

	return density;
	
}
