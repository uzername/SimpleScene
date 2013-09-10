// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util3d;

namespace WavefrontOBJViewer
{
    public class SSMesh_wfOBJ : SSMesh {
 
		protected List<SSMeshOBJSubsetData> geometrySubsets = new List<SSMeshOBJSubsetData>();
		SSAssetManagerContext ctx;
		public readonly string srcFilename;
		
		SSShaderProgram shaderPgm;
		
		// private string filename = "";
        // private bool mipmapped = false;

	    public struct SSMeshOBJSubsetData {
	   		public SSTexture diffuseTexture;
	   		public SSTexture specularTexture;
	   		public SSTexture ambientTexture;
	   		public SSTexture bumpTexture;

			public SSMaterial material;
	
			// face geometry
			public SSVertex_PosNormDiffTex1[] vertices;
	        public UInt16[] indicies;
		}

		public override string ToString ()
		{
			return string.Format ("[SSMesh_FromOBJ:{0}]", this.srcFilename);
		}
		
#region Constructor
        public SSMesh_wfOBJ(SSAssetManagerContext ctx, string filename, bool mipmapped, SSShaderProgram shaderPgm = null) {
            this.srcFilename = filename;
            // this.mipmapped = mipmapped;
            this.ctx = ctx;
            this.shaderPgm = shaderPgm;

            WavefrontObjLoader wff_data = new WavefrontObjLoader(filename,
               delegate(string resource_name) { return ctx.getAsset(resource_name).Open(); });

			Console.WriteLine("wff vertex count = {0}",wff_data.positions.Count);
			Console.WriteLine("wff face count = {0}",wff_data.numFaces);

            _makeData(wff_data);
        }    
#endregion
        
		public override void Render(){			
			foreach (SSMeshOBJSubsetData subset in this.geometrySubsets) {

				// TODO: make a GL caps abstraction to only make these calls when the caps change

				GL.Enable(EnableCap.CullFace);
				GL.Enable(EnableCap.Lighting);

                // GL.Enable(EnableCap.Blend);
                // GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
				
				GL.Color3(System.Drawing.Color.White);  // clear the vertex color to white..
				
				if (shaderPgm == null) {
					// fixed function single-texture
					GL.Enable(EnableCap.Texture2D);
					GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
				} else {
					// bind multi-texture for GLSL
					// http://adriangame.blogspot.com/2010/05/glsl-multitexture-checklist.html
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
					GL.ActiveTexture(TextureUnit.Texture1);
					GL.BindTexture(TextureTarget.Texture2D, subset.specularTexture.TextureID);
					GL.ActiveTexture(TextureUnit.Texture2);
					GL.BindTexture(TextureTarget.Texture2D, subset.ambientTexture.TextureID);
					GL.ActiveTexture(TextureUnit.Texture3);
					GL.BindTexture(TextureTarget.Texture2D, subset.bumpTexture.TextureID);
					
					// activate GLSL shader
					GL.UseProgram(shaderPgm.ProgramID);
					
					// get uniform variable handles
					int h0 = GL.GetUniformLocation(shaderPgm.ProgramID, "diffTex");
					int h1 = GL.GetUniformLocation(shaderPgm.ProgramID, "specTex");
					int h2 = GL.GetUniformLocation(shaderPgm.ProgramID, "ambiTex");
					int h3 = GL.GetUniformLocation(shaderPgm.ProgramID, "bumpTex");
					
					// bind uniform variable handles to texture-unit numbers
					GL.Uniform1(h0,0);
					GL.Uniform1(h1,1);
					GL.Uniform1(h2,2);
					GL.Uniform1(h3,3);					
				}
				
				// draw faces
				GL.Begin(BeginMode.Triangles);
				foreach(var idx in subset.indicies) {
					var vertex = subset.vertices[idx];
					GL.Color3(System.Drawing.Color.FromArgb(vertex.DiffuseColor));
					GL.TexCoord2(vertex.Tu,vertex.Tv);
					GL.Normal3(vertex.Normal);
					GL.Vertex3(vertex.Position);
                }
                GL.End();
                
                GL.UseProgram(0); // turn off GLSL
			}
		}


        private void _makeData(WavefrontObjLoader m) {
            foreach (var srcmat in m.materials) {
                if (srcmat.faces.Count != 0) {
                    this.geometrySubsets.Add(_makeMaterialSubset(m, srcmat));
                }
            }
        }
        
        private SSMeshOBJSubsetData _makeMaterialSubset(WavefrontObjLoader wff, WavefrontObjLoader.MaterialFromObj objMatSubset) {
            // create new mesh subset-data
            SSMeshOBJSubsetData subsetData = new SSMeshOBJSubsetData();            

            // setup the material...
            subsetData.material = new SSMaterial();

            // load and link every texture present 
            if (objMatSubset.diffuseTextureResourceName != null) {
                subsetData.diffuseTexture = new SSTexture(ctx.getAsset(objMatSubset.diffuseTextureResourceName));
            }
            if (objMatSubset.ambientTextureResourceName != null) {
                subsetData.ambientTexture = new SSTexture(ctx.getAsset(objMatSubset.ambientTextureResourceName));
            } 
            if (objMatSubset.bumpTextureResourceName != null) {
                subsetData.bumpTexture = new SSTexture(ctx.getAsset(objMatSubset.bumpTextureResourceName));
            }
            if (objMatSubset.specularTextureResourceName != null) {
                subsetData.specularTexture = new SSTexture(ctx.getAsset(objMatSubset.specularTextureResourceName));
            }

            // generate renderable geometry data...
            VertexSoup_VertexFormatBinder.generateDrawIndexBuffer(wff, out subsetData.indicies, out subsetData.vertices);           

			// TODO: setup VBO/IBO buffers
			// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

            return subsetData;
        }
    }
}