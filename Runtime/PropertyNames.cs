namespace TextureSwapper.Runtime
{
	public static class PropertyNames
	{
		public static readonly string[] TexturePropertyNames = new string[]
		{
			"_BaseMap", "_MainTex", "_BaseColorMap",
			"_BumpMap", "_NormalMap",
			"_MetallicGlossMap", "_MetallicSpecGlossMap", "_SpecGlossMap",
			"_ParallaxMap", "_OcclusionMap", "_DetailAlbedoMap", "_DetailNormalMap",
			"_EmissionMap", "_EmissiveColorMap",
			"_SpecularGlossMap", "_SmoothnessTexture", "_DetailMask"
		};

		public static readonly string[] EmissionPropertyNames = new string[]
		{
			"_EmissionMap", "_EmissiveColorMap"
		};
	}
}


