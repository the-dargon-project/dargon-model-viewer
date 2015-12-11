#include "types.hlsli"

cbuffer cbPerFrame : register(b0) {
	float4x4 worldViewProj;
}


PixelIn VS(VertexIn input) {
	PixelIn output;

	output.pos = mul(input.pos, worldViewProj);
	output.texCoord = input.texCoord;

	return output;
}
