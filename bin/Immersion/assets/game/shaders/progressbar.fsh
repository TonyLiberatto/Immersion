#version 330 core
//#include dither.fsh

precision highp float;

in vec2 uv;
out vec4 outColor;

uniform vec2 iResolution;
uniform float iTime;
uniform float iProgressBar;

float Circle(vec2 uv2, vec2 p, float r, float blur){
	return smoothstep(r, r - blur, length(uv2 - p));
}

vec2 RotateSpeed(float speedx, float speedy, float radius){
	return vec2(sin(iTime*speedx)*radius, cos(iTime*speedy)*radius);
}

vec2 Rotate(float rotx, float roty, float radius){
	return vec2(sin(rotx)*radius, cos(roty)*radius);
}

vec3 ChannelMix(vec3 Input, vec3 rmix, vec3 gmix, vec3 bmix)
{
	float r = (Input.r*rmix.r+Input.g*rmix.g+Input.b*rmix.b);
	float g = (Input.r*gmix.r+Input.g*gmix.g+Input.b*gmix.b);
	float b = (Input.r*bmix.r+Input.g*bmix.g+Input.b*bmix.b);
	return vec3(r,g,b);
}

vec3 ChannelMix(vec3 Input, vec3[3] arr)
{
	 return ChannelMix(Input, arr[0], arr[1], arr[2]);
}

void main() 
{
	vec2 uv2 = uv;
	uv2 -= .5;
	uv2.x *= iResolution.x / iResolution.y;
	vec4 c = vec4(1, 1, 1, 0);
	
	if (iProgressBar > 0) {
		for (float i = 0.0; i < iProgressBar; i += 0.05){
			c.a += Circle(uv2, Rotate(i*6,i*6,0.03), 0.005, 0.001);
		}
		c.a *= iProgressBar;
	}
	outColor = c;
}


