struct VertexIn {
	float4 pos         : POSITION;
	float2 texCoord    : TEXCOORD;
};

struct PixelIn {
	float4 pos      : SV_POSITION;
	float2 texCoord : TEXCOORD;
};
