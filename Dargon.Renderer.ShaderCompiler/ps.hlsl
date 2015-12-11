#include "types.hlsli"

Texture2D diffuse;
SamplerState diffuseSampler;


float4 PS(PixelIn input) : SV_Target {
	float4 outputColor = diffuse.Sample(diffuseSampler, input.texCoord);
	if (outputColor.w == 0.0f) {
		discard;
	}

	return outputColor;
}
