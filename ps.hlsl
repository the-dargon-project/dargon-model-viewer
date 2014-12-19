#include "types.hlsli"

Texture2D picture;
SamplerState pictureSampler;


float4 PS(PixelIn input) : SV_Target {
	return picture.Sample(pictureSampler, input.tex);
}
