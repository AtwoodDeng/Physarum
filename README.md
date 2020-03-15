# Physarum(Unity Demo)
## Intro

This is a Unity Demo for the Physarum Simulation. Inspired by [Sage Jenson's Procedural Art](https://sagejenson.com/physarum), this project implemented the [evolution of Physarum networks](http://eprints.uwe.ac.uk/15260/1/artl.2010.16.2.pdf) by using some CG techniques including compute shader, GPU particles and volume raymarching. A brief introduction will be presented as below.


![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/DemoCircle.png "Evolution from a circle")
![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/3D_Demo2.JPG "Evolution from a 3D Sphere")
![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/StaryNightDemo2.gif.gif "Evolution from Stary Night")
![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/MonaLisaDemo.gif "Evolution from Mona Lisa")
![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/3DDemo2.gif "Evolution from Mona Lisa")

## Features

### Editable Parameters
All Parameter in the simulation is editable.

![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/SensAngle.jpg
  "Evoulation Result in Different Sense Angle")

### Real-Time Rendering
Support up to 500,000 particles with the trail resolution of 256 * 256 * 256.

![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/50_256_withTex.JPG "Rendering result in Render Doc")

(My Device : GPU 1080Ti 11G, CPU Intel i7-8700)

### Init Particle by Texture (2D Only)

Our system supports initilize the particle system through a texture.

![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/SetInitTexMonaLisa.gif "Rendering result in Render Doc")

***

## Implemetation

## Algorithm
![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/EvolutionStep.jpg "Evolution Step")
As the image upon, the simulation contains 6 step.

* Particle Related
1. SENSE: read the data from Trail map (Render Texture)
2. ROTATE: update the particle velocity
3. MOVE: update the particle position
* Trail Related
4. DEPOSIT: change the Trail value according to the distrubution of particle
5. DIFFUSE: 'Blur' the Trail map
6. DECAY: Reduce the Trail value 

### Data Structure

![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/DataStrucure.jpg "Data Structure")

#### Particle 
ComputeBuffer, a List of ParticleInfo(particle position + particle velocity)
For 2D:
the z value is always zero. x,y repeat in [-Size,Size]

For 3D:
x,y,z repeat in [-Size,Size]
### Trail
For 2D:
2D Render Texture * 3
TrailRead/TrailWrite: two render texture for trail update 
Deposit : an additional render texture used in deposit step

For3D:
3D Render Texture * 3 
The same as 2D

## Visualization

### Particle
The GPU particle is used to visulize the cell. The implementation is similar to [Robert-K's project](https://github.com/Robert-K/gpu-particles/blob/master). See the [code](https://github.com/AtwoodDeng/Physarum/blob/master/Assets/AtPhysarum/Shader/BillboardParticles.shader) in the project.

![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/SimpleMove.gif.gif "Simple Move")

### 2D Trail
The trail texture is used as a main texture in a unlit shader. A LUT is added to exhibit the variation of the density. See the [code](https://github.com/AtwoodDeng/Physarum/blob/master/Assets/AtPhysarum/Shader/VisualizeTrail.shader)

![image](https://github.com/AtwoodDeng/Physarum/blob/master/Demo/LUT.jpg "Before/After the LUT is added")

### 3D Trail
3D Trail's visulization is volume raymarching. It's basically a post-process effect, which used to visulize the 3D Trail render texture. See the [code](https://github.com/AtwoodDeng/Physarum/blob/master/Assets/AtPhysarum/AtPhysarum3D/Shader/VolumeShader.shader)

## Reference

[1]Physarum, Sage Jenson,(https://sagejenson.com/physarum)

[2]Physarum, Entagma, (https://entagma.com/physarum-slime-mold/)

[3]Characteristics of pattern formation and evolution in approximations of Physarum transport networks, Jeff Jones, 2011 (https://uwe-repository.worktribe.com/output/980579)

[4]GPU Particle, Robert-K(https://github.com/Robert-K/gpu-particles/blob/master)

## License

[![License: CC BY-SA 4.0](https://img.shields.io/badge/License-CC%20BY--SA%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-sa/4.0/)

