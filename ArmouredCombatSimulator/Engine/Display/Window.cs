using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector3 = System.Numerics.Vector3;
using System.Numerics;
using OpenTK.Windowing.Common.Input;

namespace Engine
{
    public class Window : GameWindow
    {
        int VertexBufferObject;

        int VertexArrayObject;

        int ElementBufferObject;

        Canon _2a46m = new Canon("Engine/Display/Models/2A46M.glb");
        float modelRotation;
        float finalRotation;
        
        Shader shader;

        Camera camera = new Camera(new Vector3(0, 0, 3));

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) { }

        protected override void OnLoad()
        {
            base.OnLoad();

            CursorState = CursorState.Grabbed;
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.Enable(EnableCap.DepthTest);

            GL.ClearColor(0f, 0f, 0f, 1.0f);

            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _2a46m.Vertices().Length * sizeof(float), _2a46m.Vertices(), BufferUsageHint.StaticDraw);

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _2a46m.Indices().Length * sizeof(uint), _2a46m.Indices(), BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            shader = new Shader("Engine/Display/Shader/shader.vert", "Engine/Display/Shader/shader.frag");
            shader.Use();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(VertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, _2a46m.Indices().Count(), DrawElementsType.UnsignedInt, 0);

            float aspectRatio = 1000.0f / 800.0f;

            Matrix4x4 view = camera.GetViewMatrix();
            Matrix4x4 projection = camera.GetProjectionMatrix(aspectRatio);
            Matrix4x4 model = Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(finalRotation));;

            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);

            shader.SetVector3("lightDir", new Vector3(-0.3f, -1.0f, -0.5f));     // like sunlight from above
            shader.SetVector3("lightColor", new Vector3(1.0f, 1.0f, 1.0f));      // white sunlight
            shader.SetVector3("objectColor", new Vector3(0.7f, 0.7f, 0.7f));   
            shader.Use();

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var input = KeyboardState;

            Vector3 direction = Vector3.Zero;
            if (input.IsKeyDown(Keys.W)) direction += camera.Front;
            if (input.IsKeyDown(Keys.S)) direction -= camera.Front;
            if (input.IsKeyDown(Keys.A)) direction -= Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up));
            if (input.IsKeyDown(Keys.D)) direction += Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up));
            if (input.IsKeyDown(Keys.Space)) direction += camera.Up;
            if (input.IsKeyDown(Keys.LeftShift)) direction -= camera.Up;

            if (direction != Vector3.Zero)
                camera.Move(direction, (float)e.Time);

            if (input.IsKeyDown(Keys.R) && !(modelRotation >= 100))
            {
                modelRotation += 20f * (float)e.Time; // 20% per second
                finalRotation = modelRotation / 100 * 90;
            }
            if (input.IsKeyDown(Keys.F) && !(modelRotation <= 0))
            {
                modelRotation -= 20f * (float)e.Time; // 20% per second
                finalRotation = modelRotation / 100 * 90;
            }
            
            if (input.IsKeyDown(Keys.LeftAlt)) CursorState = CursorState.Normal;
            else CursorState = CursorState.Grabbed;

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
        }

        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VertexBufferObject);

            base.OnUnload();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            camera.Rotate(e.DeltaX, e.DeltaY);
        }
    }
    
    public class Camera
    {
        public Vector3 Position { get; private set; }
        public Vector3 Front { get; private set; } = Vector3.UnitZ * -1 * 2;
        public Vector3 Up { get; private set; } = Vector3.UnitY * 2;

        private float yaw = -90f;   // yaw points -Z at start
        private float pitch = 0f;
        private float fov = 80f;

        public Camera(Vector3 startPos)
        {
            Position = startPos;
            UpdateVectors();
        }

        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
        }

        public Matrix4x4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI * fov / 180f, aspectRatio, 0.1f, 100f);
        }

        public void Move(Vector3 direction, float deltaTime, float speed = 2.5f)
        {
            Position += direction * speed * deltaTime;
        }

        public void Rotate(float deltaX, float deltaY, float sensitivity = 0.1f)
        {
            yaw += deltaX * sensitivity;
            pitch -= deltaY * sensitivity;

            pitch = Math.Clamp(pitch, -89f, 89f);
            UpdateVectors();
        }

        private void UpdateVectors()
        {
            float yawRad = MathF.PI / 180f * yaw;
            float pitchRad = MathF.PI / 180f * pitch;

            Front = Vector3.Normalize(new Vector3(
                MathF.Cos(yawRad) * MathF.Cos(pitchRad),
                MathF.Sin(pitchRad),
                MathF.Sin(yawRad) * MathF.Cos(pitchRad)
            ));
        }
    }
}