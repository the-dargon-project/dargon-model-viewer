struct VertexIn {
	float4 pos    : POSITION;
	float4 normal : NORMAL;
	float2 tex    : TEXCOORD;
};

struct PixelIn {
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD;
};
