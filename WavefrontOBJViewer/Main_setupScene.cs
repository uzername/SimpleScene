// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using SimpleScene;

namespace WavefrontOBJViewer
{
	partial class WavefrontOBJViewer : OpenTK.GameWindow
	{
		public void setupScene() {
			scene.MainShader = mainShader;
			scene.PssmShader = pssmShader;
			scene.InstanceShader = instancingShader;
			scene.FrustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
			scene.BeforeRenderObject += (obj, renderConfig) => {
				mainShader.Activate();
				if (obj == selectedObject) {
					renderConfig.drawWireframeMode = WireframeMode.GLSL_SinglePass;
					mainShader.UniShowWireframes = true;			

				} else {
					renderConfig.drawWireframeMode = WireframeMode.None;
					mainShader.UniShowWireframes = false;

				}
			};


			// 0. Add Lights
			var light = new SSDirectionalLight (LightName.Light0);
			light.Direction = new Vector3(0f, 0f, -1f);
			scene.AddLight(light);

			#if false
			light.ShadowMap = new SSSimpleShadowMap (TextureUnit.Texture4);
			var smapDebug = new SSObjectHUDQuad (light.ShadowMap.TextureID);
			smapDebug.Scale = new Vector3(0.3f);
			smapDebug.Pos = new Vector3(50f, 200, 0f);
			hudScene.AddObject(smapDebug);
			#endif


			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");
						
			// add drone
			SSObject droneObj = new SSObjectMesh (mesh);
			scene.AddObject (this.activeModel = droneObj);
			droneObj.renderState.lighted = true;
			droneObj.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.ShininessMatColor = 10.0f;
			//droneObj.EulerDegAngleOrient(-40.0f,0.0f);
			droneObj.Pos = new OpenTK.Vector3(0,0,-15f);
			droneObj.Name = "drone 1";

			// add second drone
			
			SSObject drone2Obj = new SSObjectMesh(
				SSAssetManager.GetInstance<SSMesh_wfOBJ>("./drone2/", "Drone2.obj")
			);
			scene.AddObject (drone2Obj);
			drone2Obj.renderState.lighted = true;
			drone2Obj.AmbientMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.ShininessMatColor = 10.0f;
			drone2Obj.EulerDegAngleOrient(-40f, 0f);
			drone2Obj.Pos = new OpenTK.Vector3(0f, 0f, 0f);
			drone2Obj.Name = "drone 2";

			// last for the main scene. Add Camera

			var camera = new SSCameraThirdPerson (droneObj);
			camera.followDistance = 30.0f;
			scene.ActiveCamera = camera;
			scene.AddObject (camera);


			// setup a sun billboard object and a sun flare spriter renderer
			{
				var sunDisk = new SSMeshDisk ();
				var sunBillboard = new SSObjectBillboard (sunDisk, true);
				sunBillboard.MainColor = new Color4 (1f, 1f, 0.8f, 1f);
				sunBillboard.Pos = new Vector3 (0f, 0f, 18000f);
				sunBillboard.Scale = new Vector3 (600f);
				sunBillboard.renderState.frustumCulling = false;
				sunBillboard.renderState.lighted = false;
				sunBillboard.renderState.castsShadow = false;
				sunDiskScene.AddObject(sunBillboard);

				SSTexture flareTex = SSAssetManager.GetInstance<SSTextureWithAlpha>("./Planets/", "sun_flare.png");
				const float bigOffset = 0.8889f;
				const float smallOffset = 0.125f;
				RectangleF[] flareSpriteRects = {
					new RectangleF(0f, 0f, 1f, bigOffset),
					new RectangleF(0f, bigOffset, smallOffset, smallOffset),
					new RectangleF(smallOffset, bigOffset, smallOffset, smallOffset),
					new RectangleF(smallOffset*2f, bigOffset, smallOffset, smallOffset),
					new RectangleF(smallOffset*3f, bigOffset, smallOffset, smallOffset),
				};
				float[] spriteScales = { 20f, 1f, 2f, 1f, 1f };
				var sunFlare = new SSObjectSunFlare (sunDiskScene, sunBillboard, flareTex, 
													 flareSpriteRects, spriteScales);
				sunFlare.Scale = new Vector3 (2f);
				sunFlare.renderState.lighted = false;
				sunFlareScene.AddObject(sunFlare);
			}

			// particle system test
			// particle systems should be drawn last (if it requires alpha blending)
			{
				// setup an emitter
				var box = new ParticlesSphereGenerator (new Vector3(0f, 0f, 0f), 10f);
				var emitter = new SSParticlesFieldEmitter (box);
				emitter.ParticlesPerEmission = 1000;
				emitter.EmissionInterval = 10000f;
				emitter.Life = 1000f;
				emitter.ColorComponentMin = new Color4 (0.5f, 0.5f, 0.5f, 1f);
				emitter.ColorComponentMax = new Color4 (1f, 1f, 1f, 1f);
				emitter.VelocityComponentMax = new Vector3 (.3f);
				emitter.VelocityComponentMin = new Vector3 (-.3f);
				emitter.AngularVelocityMin = new Vector3 (-0.5f);
				emitter.AngularVelocityMax = new Vector3 (0.5f);
				RectangleF[] uvRects = new RectangleF[18*6];
				float tileWidth = 1f / 18f;
				float tileHeight = 1f / 6f;
				for (int r = 0; r < 6; ++r) {
					for (int c = 0; c < 18; ++c) {
						uvRects [r*18 + c] = new RectangleF (tileWidth * (float)r, 
							tileHeight * (float)c,
							tileWidth, 
							tileHeight);
					}
				}
				emitter.SpriteRectangles = uvRects;


				// make a particle system
				SSParticleSystem ps = new SSParticleSystem (1000);
				ps.AddEmitter(emitter);
				ps.EmitAll();

				// test a renderer
				//var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>("./Planets/", "planet-14.png");
				//var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>("./Planets/", "sun.png");
				var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "elements.png");
				var renderer = new SSInstancedMeshRenderer (ps, tex, SSTexturedCube.Instance);
				renderer.Pos = new Vector3 (0f, 0f, -20f);
				renderer.AlphaBlendingEnabled = false;
				renderer.Billboarding = SSInstancedMeshRenderer.BillboardingType.None;
				scene.AddObject(renderer);
			}
		}

		public void setupEnvironment() {

			// add skybox cube
			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./skybox/","skybox.obj");
			SSObject skyboxCube = new SSObjectMesh(mesh);
			environmentScene.AddObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);
			skyboxCube.renderState.lighted = false;

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SSObjectMesh(new SSMesh_Starfield(1600));
			environmentScene.AddObject(skyboxStars);
			skyboxStars.renderState.lighted = false;

		}


		SSObjectGDISurface_Text fpsDisplay;

		SSObjectGDISurface_Text wireframeDisplay;

		public void updateWireframeDisplayText(WireframeMode mode) {
			wireframeDisplay.Label = String.Format ("press 'w' to toggle wireframe mode: [{0}]", 
				mode);
		}

		public void setupHUD() {
			hudScene.ProjectionMatrix = Matrix4.Identity;

			// HUD Triangle...
			//SSObject triObj = new SSObjectTriangle ();
			//hudScene.addObject (triObj);
			//triObj.Pos = new Vector3 (50, 50, 0);
			//triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			fpsDisplay = new SSObjectGDISurface_Text ();
			fpsDisplay.Label = "FPS: ...";
			hudScene.AddObject (fpsDisplay);
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
			fpsDisplay.Scale = new Vector3 (1.0f);

			// wireframe mode text....
			wireframeDisplay = new SSObjectGDISurface_Text ();
			hudScene.AddObject (wireframeDisplay);
			wireframeDisplay.Pos = new Vector3 (10f, 40f, 0f);
			wireframeDisplay.Scale = new Vector3 (1.0f);
			updateWireframeDisplayText (scene.DrawWireFrameMode);

			// HUD text....
			var testDisplay = new SSObject2DSurface_AGGText ();
			testDisplay.Label = "TEST AGG";
			hudScene.AddObject (testDisplay);
			testDisplay.Pos = new Vector3 (50f, 100f, 0f);
			testDisplay.Scale = new Vector3 (1.0f);
		}
	}
}