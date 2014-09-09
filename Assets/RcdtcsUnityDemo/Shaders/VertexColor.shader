Shader "Custom/VertexColorShader" {
	Category {
		BindChannels {
			Bind "Color", color
		}
		SubShader {
    		Pass {}
		}
	}
}