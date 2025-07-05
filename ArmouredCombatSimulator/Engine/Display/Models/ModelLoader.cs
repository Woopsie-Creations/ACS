using SharpGLTF.Schema2;

namespace Engine
{
    class ModelLoader
    {
        public static void LoadGlbModel(string path, out float[] vertices, out uint[] indices)
        {
            var model = ModelRoot.Load(path);

            var positionsList = new List<float>();
            var indicesList = new List<uint>();

            uint vertexOffset = 0;

            foreach (var node in model.DefaultScene.VisualChildren)
            {
                TraverseNode(node, ref vertexOffset, positionsList, indicesList);
            }

            vertices = positionsList.ToArray();
            indices = indicesList.ToArray();
        }

        private static void TraverseNode(Node node, ref uint vertexOffset, List<float> vertices, List<uint> indices)
        {
            if (node.Mesh != null)
            {
                foreach (var primitive in node.Mesh.Primitives)
                {
                    var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();
                    var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                    var localIndices = primitive.IndexAccessor.AsIndicesArray();

                    for (int i = 0; i < positions.Count; i++)
                    {
                        var pos = positions[i];
                        var normal = normals[i];

                        vertices.Add(pos.X);
                        vertices.Add(pos.Y);
                        vertices.Add(pos.Z);

                        vertices.Add(normal.X);
                        vertices.Add(normal.Y);
                        vertices.Add(normal.Z);
                    }

                    foreach (var i in localIndices)
                    {
                        indices.Add(vertexOffset + i);
                    }

                    vertexOffset += (uint)positions.Count;
                }
            }

            foreach (var child in node.VisualChildren)
            {
                TraverseNode(child, ref vertexOffset, vertices, indices);
            }
        }
    }

    class Canon
    {
        private float[] vertices;
        private uint[] indices;

        public Canon(string path)
        {
            ModelLoader.LoadGlbModel(path, out vertices, out indices);
        }

        public float[] Vertices() => vertices;
        public uint[] Indices() => indices;

        
    }
}