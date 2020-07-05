using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace CameraPlus
{
    public class SaberSalient
    {
        private static SaberSalient _singleton;
        private static System.Object syncRoot = new System.Object();

        public AssetBundle ab;

        private IntPtr native;
        private Matrix4x4 mvpTransform;
        private float[] mvpTransformRaw;

        private Matrix4x4 mvTransform;
        private float[] positionRaw;
        private float[] rotationRaw;


        public Matrix4x4 pTransform;

        private readonly int bufferSize;

        public struct CameraIntrinsics
        {
            /** Width of a frame in pixels. */
            public int width;
            /** Height of a frame in pixels. */
            public int height;
            /** Horizontal coordinate of the principal point of a frame, as a pixel offset from the left edge. */
            public float ppx;
            /** Vertical coordinate of the principal point of a frame, as a pixel offset from the top edge. */
            public float ppy;
            /** Focal length of the image plane, as a multiple of pixel width */
            public float fx;
            /** Focal length of the image plane, as a multiple of pixel height */
            public float fy;
        };

        public CameraIntrinsics Camera;

        private SaberSalient()
        {
            if (SaberSalientNative.Init(out native) != 0)
            {
                native = IntPtr.Zero;
                throw new ExternalException("Could not init native saber-saliend.dll");
            }
            mvpTransformRaw = new float[16];
            mvpTransform = Matrix4x4.identity;

            positionRaw = new float[3];
            rotationRaw = new float[9];
            mvTransform = Matrix4x4.identity;
            pTransform = Matrix4x4.zero;

            if (SaberSalientNative.GetCameraIntrinsics(native, Camera) != 0)
                throw new ExternalException("Could not get camera intrinsics in native saber-saliend.dll");

            Logger.Log("Initialized SaberSalient camera: " + Camera.width + " " + Camera.height);
            float near = 0.1f;
            float far = 10.0f;
            pTransform.m00 = 2 * Camera.fx / Camera.width;
            pTransform.m11 = 2 * Camera.fy / Camera.height;
            pTransform.m22 = -(far + near) / (far - near);
            pTransform.m32 = -1;
            pTransform.m23 = 2 * far * near / (near - far);

            CurrentColorTex = new Texture2D(Camera.width, Camera.height, TextureFormat.ARGB32, false, false);
            CurrentDepthTex = new Texture2D(Camera.width, Camera.height, TextureFormat.RFloat, false, true);
            bufferSize = Camera.width * Camera.height * 4;

            ab = AssetBundle.LoadFromMemory(Utils.GetResource(System.Reflection.Assembly.GetExecutingAssembly(), "CameraPlus.Resources.sabersalient"));
               // AssetBundle.LoadFromFile("G:\\prorgamming\\CameraPlus\\CameraPlus\\Resources\\sabersalient");
            if (ab == null)
                throw new NullReferenceException("SaberSalientOverlayed - No AssetBundle!");
        }

        ~SaberSalient()
        {
            if (native != IntPtr.Zero)
                SaberSalientNative.Destroy(native);
            ab.Unload(true);
            Logger.Log("Destroyed SaberSalient.");
        }

        public Matrix4x4 GetMvpTransform()
        {
            if (SaberSalientNative.GetCurrentTransform(native, mvpTransformRaw) != 0)
                return mvpTransform;

            for (int i = 0; i < 16; i++)
                mvpTransform[i] = mvpTransformRaw[i];

            return mvpTransform;
        }

        public Matrix4x4 GetMvTransform()
        {
            if (SaberSalientNative.GetCurrentPosition(native, positionRaw) != 0)
                return mvTransform;

            if (SaberSalientNative.GetCurrentRotation(native, rotationRaw) != 0)
                return mvTransform;

            for (int i = 0; i < 3; i++)
            {
                mvTransform[12 + i] = positionRaw[i];
                for (int j = 0; j < 3; j++)
                {
                    mvTransform[i + 4 * j] = rotationRaw[i + 3 * j];
                }
            }

            return mvTransform;
        }

        public Texture2D CurrentColorTex { get; }
        public Texture2D CurrentDepthTex { get; }

        public void UpdateBuffers()
        {
            CurrentColorTex.LoadRawTextureData(SaberSalientNative.GetColorBuf(native), bufferSize);
            CurrentDepthTex.LoadRawTextureData(SaberSalientNative.GetDepthBuf(native), bufferSize);
            CurrentColorTex.Apply();
            CurrentDepthTex.Apply();
        }

        public static SaberSalient Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    lock (syncRoot)
                    {
                        if (_singleton == null)
                            _singleton = new SaberSalient();
                    }
                }
                return _singleton;
            }
        }
    }

    internal static class SaberSalientNative
    {
        private const string _dllname = "saber-salient.dll";

        [DllImport(_dllname, EntryPoint = "SaberSalient_init")]
        public static extern int Init(out IntPtr out_SaberSalient);

        [DllImport(_dllname, EntryPoint = "SaberSalient_destroy")]
        public static extern void Destroy(IntPtr in_SaberSalient);

        [DllImport(_dllname, EntryPoint = "SaberSalient_cameraIntrinsics")]
        public static extern int GetCameraIntrinsics(IntPtr in_SaberSalient, in SaberSalient.CameraIntrinsics out_intrinsics);

        [DllImport(_dllname, EntryPoint = "SaberSalient_currentTransform")]
        public static extern int GetCurrentTransform(IntPtr in_SaberSalient, float[] out_mat44);

        [DllImport(_dllname, EntryPoint = "SaberSalient_currentPosition")]
        public static extern int GetCurrentPosition(IntPtr in_SaberSalient, float[] out_vec3);

        [DllImport(_dllname, EntryPoint = "SaberSalient_currentRotation")]
        public static extern int GetCurrentRotation(IntPtr in_SaberSalient, float[] out_mat33);

        [DllImport(_dllname, EntryPoint = "SaberSalient_getColorBuf")]
        public static extern IntPtr GetColorBuf(IntPtr in_SaberSalient);

        [DllImport(_dllname, EntryPoint = "SaberSalient_getDepthBuf")]
        public static extern IntPtr GetDepthBuf(IntPtr in_SaberSalient);
    }

    public class SaberSalientComponent : MonoBehaviour
    {
        public readonly SaberSalient ss;

        public SaberSalientComponent()
        {
            ss = SaberSalient.Singleton;
            enabled = true;
        }

        protected virtual void Update() => ss.UpdateBuffers();
    }

    public class SaberSalientCamera : MonoBehaviour
    {
        private SaberSalient ss;
        private Material material;


        public virtual void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (material == null)
                throw new NullReferenceException("Render - No material!");
            if (ss == null)
                throw new NullReferenceException("Render - No SaberSalient!");
            if (ColorTex == null)
                throw new NullReferenceException("Render - No ColorTex!");
            if (DepthTex == null)
                throw new NullReferenceException("Render - No DepthTex!");
            Graphics.Blit(src, dst, material);
        }

        public void Init(SaberSalient ss, bool opaque)
        {
            this.ss = ss;
            if (opaque)
            {
                material = ss.ab.LoadAsset<Material>("Hidden_SaberOpaque");
                if (material == null)
                    throw new NullReferenceException("SaberSalientCamera - No material!");
                material.SetTexture("_ColorTex", ColorTex);
            } else
            {
                material = ss.ab.LoadAsset<Material>("Hidden_SaberSalient");
                if (material == null)
                    throw new NullReferenceException("SaberSalientCamera - No material!");
                material.SetTexture("_ColorTex", ColorTex);
                material.SetTexture("_DepthTex", DepthTex);
            }
            enabled = true;
        }

        public SaberSalient.CameraIntrinsics Camera => ss.Camera;
        public Texture2D ColorTex => ss.CurrentColorTex;
        public Texture2D DepthTex => ss.CurrentDepthTex;
        public Matrix4x4 MvTransform => ss.GetMvTransform();
        public Matrix4x4 PTransform => ss.pTransform;
    }
}
